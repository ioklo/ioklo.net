using System;

namespace Homepage.Models
{
    public struct UserId
    {
        public int Value { get; }
        public UserId(int value) { Value = value; }

        public override bool Equals(object obj)
        {
            return obj is UserId id &&
                   Value == id.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(UserId left, UserId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UserId left, UserId right)
        {
            return !(left == right);
        }
    }
}