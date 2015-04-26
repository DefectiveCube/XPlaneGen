﻿using System;

namespace XPlaneGenConsole.Data
{
	public struct Fahrenheit : ITemperature
	{
		public const float MinValue = -459.67f;

		internal float value; 

		internal Fahrenheit(float value){

			if (value < MinValue || float.IsNaN (value) || float.IsInfinity (value)) {
				throw new ArgumentOutOfRangeException ();
			}			

			this.value = value;
		}

        public override bool Equals(object obj)
        {
            return value == ((Fahrenheit)obj).value;
        }

        public override int GetHashCode()
        {
            return Convert.ToInt32(value);
        }

        public TypeCode GetTypeCode()
		{
			return TypeCode.Single;
		}

		public int CompareTo(float value)
		{
			return this.value.CompareTo (value);
		}

		public bool Equals(float value)
		{
			return this.value == value;
		}

		bool IConvertible.ToBoolean (IFormatProvider provider)
		{
			return Convert.ToBoolean (value);
		}

		byte IConvertible.ToByte (IFormatProvider provider)
		{
			return Convert.ToByte (value);
		}

		char IConvertible.ToChar (IFormatProvider provider)
		{
			return Convert.ToChar (value);
		}

		DateTime IConvertible.ToDateTime (IFormatProvider provider)
		{
			return Convert.ToDateTime (value);
		}

		decimal IConvertible.ToDecimal (IFormatProvider provider)
		{
			return Convert.ToDecimal (value);
		}

		double IConvertible.ToDouble (IFormatProvider provider)
		{
			return Convert.ToDouble (value);
		}

		short IConvertible.ToInt16 (IFormatProvider provider)
		{
			return Convert.ToInt16 (value);
		}

		int IConvertible.ToInt32 (IFormatProvider provider)
		{
			return Convert.ToInt32 (value);
		}

		long IConvertible.ToInt64 (IFormatProvider provider)
		{
			return Convert.ToInt64 (value);
		}

		sbyte IConvertible.ToSByte (IFormatProvider provider)
		{
			return Convert.ToSByte (value);
		}

		float IConvertible.ToSingle (IFormatProvider provider)
		{
			return Convert.ToSingle (value);
		}
			
		public string ToString (IFormatProvider provider)
		{
			return value.ToString ();
		}

		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			return Convert.ChangeType (value, conversionType);
		}

		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return Convert.ToUInt16 (value);
		}

		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return Convert.ToUInt32 (value);
		}

		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return Convert.ToUInt64 (value);
		}

		//
		// Operators
		//
		public static bool operator == (Fahrenheit left, Fahrenheit right) {
			return left.value == right.value;
		}

		public static bool operator > (Fahrenheit left, Fahrenheit right) {
			return left.value > right.value;
		}

		public static bool operator >= (Fahrenheit left, Fahrenheit right) {
			return left.value >= right.value;
		}

		public static bool operator != (Fahrenheit left, Fahrenheit right) {
			return left.value != right.value;
		}

		public static bool operator < (Fahrenheit left, Fahrenheit right) {
			return left.value < right.value;
		}

		public static bool operator <= (Fahrenheit left, Fahrenheit right) {
			return left.value <= right.value;
		}

		//
		// implicit conversions
		//
		public static implicit operator Fahrenheit (int value)
		{
			if (value < MinValue) {
				throw new ArgumentOutOfRangeException ();
			}

			var temp = new Fahrenheit ();
			temp.value = Convert.ToSingle (value);

			return temp;
		}

		public static implicit operator Fahrenheit (float value)
		{
			if (value < MinValue) {
				throw new ArgumentOutOfRangeException ();
			}

			return new Fahrenheit (value);
		}

		public static implicit operator Fahrenheit (double value)
		{
			if (value < MinValue) {
				throw new ArgumentOutOfRangeException ();
			}

			return new Fahrenheit ((float)value);
		}

		public static implicit operator float (Fahrenheit value)
		{
			return value.value;
		}

        public static implicit operator Fahrenheit(Celsius value)
        {
            var temp = new Fahrenheit();
            temp.value = value.value * 9.0f / 5.0f + 32;

            return temp;
        }
    }
}