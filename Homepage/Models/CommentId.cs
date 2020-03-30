using System;

namespace Homepage.Models
{
    public struct CommentId
    {
        public int Value { get; }
        public CommentId(int value) { Value = value; }

        public override bool Equals(object obj)
        {
            return obj is CommentId id &&
                   Value == id.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(CommentId left, CommentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CommentId left, CommentId right)
        {
            return !(left == right);
        }
    }
}