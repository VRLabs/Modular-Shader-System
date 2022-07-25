using System;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    /// <summary>
    /// Object that represents a default texture for a texture property.
    /// </summary>
    [Serializable]
    public class DefaultTexture : IEquatable<DefaultTexture>
    {
        /// <summary>
        /// Name of the property.
        /// </summary>
        public string PropertyName;

        /// <summary>
        /// Default texture value.
        /// </summary>
        public Texture Texture;

        public bool Equals(DefaultTexture other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return PropertyName == other.PropertyName && Equals(Texture, other.Texture);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DefaultTexture)obj);
        }
        
        public static bool operator == (DefaultTexture left, DefaultTexture right)
        {
            return left?.Equals(right) ?? right is null;
        }

        public static bool operator !=(DefaultTexture left, DefaultTexture right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((PropertyName != null ? PropertyName.GetHashCode() : 0) * 397) ^ (Texture != null ? Texture.GetHashCode() : 0);
            }
        }
    }
}