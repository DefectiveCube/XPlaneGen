﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XPlaneGenConsole;

namespace XPlaneGenConsole
{
	public class CsvConverter<T>
        where T: BinaryDatapoint, new()
	{
        private static Barrier barrier;
        private static Barrier startBarrier;
        private static ConcurrentQueue<string> InputQueue;
        private static ConcurrentQueue<T> OutputQueue;
        private static ConcurrentQueue<Tuple<DateTime, int>> FlightTimes;
        private static int validLineCount = 0;
        private static int Fields = typeof(T).GetCustomAttribute<CsvRecordAttribute>().Count;
        private static Action<T, string[]> parser = CsvParser.GetParser<T>();
        private static StringBuilder messages = new StringBuilder();

        public static event EventHandler<string> MessageWritten, ErrorWritten;
        public static event EventHandler ReadStarted, ReadCompleted, ParsingStarted, ParsingCompleted, WriteStarted, WriteCompleted;

        private static readonly object _locker = new object();

        public static async Task LoadAsync(string path, string outputPath)
        {
            await Task.Factory.StartNew(() => Load(path, outputPath), CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static void Load(string path, string outputDirectory)
        {
            var hash = XPlaneGenConsole.IO.Hash.ComputeHash(path);
            outputDirectory = System.IO.Path.Combine(outputDirectory, BitConverter.ToString(hash).Replace("-", ""));

            barrier = new Barrier(Environment.ProcessorCount + 1);
            startBarrier = new Barrier(Environment.ProcessorCount + 1);

            InputQueue = new ConcurrentQueue<string>();
            OutputQueue = new ConcurrentQueue<T>();
            FlightTimes = new ConcurrentQueue<Tuple<DateTime, int>>();

            messages.Clear();

            // Reader thread
            Thread producer = new Thread(ProducerThread);

            // Writer thread
            Thread consumer = new Thread(ConsumerThread);

            // Worker threads
            Thread[] threads = new Thread[Environment.ProcessorCount];

            var start = DateTime.Now;

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(ThreadRead);
                threads[i].Start();
            }

            //ReadStarted(null, new EventArgs());
            //ParsingStarted(null, new EventArgs());

            // Start reading
            producer.Start(path);
            producer.Join(); // Producer has a barrier with the worker threads, so this blocks until all workers are done

            if (ReadCompleted != null)
            {
                ReadCompleted(null, new EventArgs());
            }

            if (ParsingCompleted != null)
            {
                ParsingCompleted(null, new EventArgs());
            }

            messages.AppendFormat("Valid Lines: {0}", validLineCount);
            messages.AppendLine();
            messages.AppendFormat("Unique Flights: {0}", FlightTimes.Count);
            messages.AppendLine();
            messages.AppendFormat("Process Completed in {0} seconds", DateTime.Now.Subtract(start).TotalSeconds);
            messages.AppendLine();

            // Start writing
            consumer.Start(outputDirectory);
            consumer.Join();

            if (WriteCompleted != null)
            {
                WriteCompleted(null, EventArgs.Empty);
            }

            if (MessageWritten != null)
            {
                MessageWritten(null, messages.ToString());
            }


            OutputQueue = null;
            FlightTimes = null;
        }

        static void ConsumerThread(object path)
        {
            var write = BinaryDatapoint.GetWriteAction<T>();
            string filePath = path as string;

            var ordered = from dp in OutputQueue
                          orderby dp.DateTime, dp.Timestamp
                          select dp;

            /* build Flight index */
            var indexPath = filePath.Replace(".output", ".index");

            /* write file data */

            var ms = new MemoryStream();
            byte[] data = new byte[] { };
            long compressSize, normalSize;

            //Console.WriteLine("Writing to {0}", filePath);
            //Console.WriteLine("Consumer found {0} datapoints", validLineCount);

            using (var writer = new BinaryWriter(ms))
            {
                foreach (var dp in ordered)
                {
                    write(dp, writer);
                }

                data = ms.ToArray();
                normalSize = ms.Length;
            }

            using (var file = File.Open(filePath, FileMode.Create))            
            using (var compress = new GZipStream(file, CompressionMode.Compress))
            {
                compress.Write(data, 0, data.Length);
                compressSize = compress.BaseStream.Length;
            }

            lock (_locker)
            {
                messages.AppendFormat("Uncompressed Size: {0} bytes", normalSize);
                messages.AppendLine();
                messages.AppendFormat("Compressed Size: {0} bytes", compressSize);
                messages.AppendLine();
                messages.AppendFormat("Compression Ratio: {0:P}", 1.0 - (double)compressSize / normalSize);
                messages.AppendLine();
            }
        }

        static void ProducerThread(object path)
        {
            if (!File.Exists(path as string))
            {
                throw new FileNotFoundException();
            }

            StreamReader reader = new StreamReader(path as string);
            int count = 0;

            bool AreThreadsSet = false;

            using (reader)
            {
                messages.AppendFormat("File size: {0} bytes", reader.BaseStream.Length);
                messages.AppendLine();
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    InputQueue.Enqueue(reader.ReadLine());
                    count++;

                    if (!AreThreadsSet)
                    {
                        AreThreadsSet = true;
                        startBarrier.SignalAndWait();
                    }
                }
            }

            barrier.SignalAndWait();
            InputQueue = null;
        }

		static void ThreadRead()
		{
			string result;
			var tid = Thread.CurrentThread.ManagedThreadId;

            // Wait for the go signal
            try
            {
                startBarrier.SignalAndWait();
            }
            catch (EntryPointNotFoundException ex)
            {
                startBarrier.RemoveParticipant();
                Thread.CurrentThread.Abort();
                Console.WriteLine(ex.Message);
                return;
            }

            while (!InputQueue.IsEmpty)
            {
                if (InputQueue.TryDequeue(out result))
                {
                    var value = result.Split(new char[] { ',' }, StringSplitOptions.None);

                    if (value.Length == Fields)
                    {
                        T dp = Activator.CreateInstance<T>();
                        parser(dp, value);

                        OutputQueue.Enqueue(dp);
                        Interlocked.Increment(ref validLineCount);
                    }
                    else if (value.Length == 4 && value[3].Contains("POWER ON"))
                    {
                        FlightTimes.Enqueue(
                            new Tuple<DateTime, int>(
                                value[1].AsDateTime(value[2].AsTimeSpan()),
                                value[3].AsInt()
                                ));
                    }
                    else
                    {
                        Console.WriteLine("Skipping {0}", value.Length);
                    }
                }
            }

            barrier.SignalAndWait();
		}
	}
}