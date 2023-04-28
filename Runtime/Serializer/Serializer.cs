﻿using System;
using System.Collections.Generic;

namespace DataUtilities.Serializer
{
    /// <summary>
    /// This class handles serialization to a raw binary data array.
    /// To start the process, create an instance.
    /// You can then call the instance methods like <see cref="Serialize(int)"/> or <see cref="Serialize(string)"/>.
    /// When you're done, you can extract the created byte array from the <see cref="Result"/> property.
    /// </summary>
    public class Serializer
    {
        readonly List<byte> result = new();

        public byte[] Result => result.ToArray();

        readonly Dictionary<Type, Delegate> typeSerializers;

        delegate void TypeSerializer<T>(T v);
        static KeyValuePair<Type, Delegate> GenerateTypeSerializer<T>(TypeSerializer<T> typeSerializer) => new(typeof(T), typeSerializer);

        public Serializer()
        {
            typeSerializers = (new KeyValuePair<Type, Delegate>[]
            {
                GenerateTypeSerializer<int>(Serialize),
                GenerateTypeSerializer<int>(Serialize),
                GenerateTypeSerializer<float>(Serialize),
                GenerateTypeSerializer<bool>(Serialize),
                GenerateTypeSerializer<byte>(Serialize),
                GenerateTypeSerializer<short>(Serialize),
                GenerateTypeSerializer<char>(Serialize),
                GenerateTypeSerializer<string>(Serialize),
                GenerateTypeSerializer<double>(Serialize),
                GenerateTypeSerializer<ReadableFileFormat.Value>(Serialize),
            }).ToDictionary();
        }

        TypeSerializer<T> GetSerializerForType<T>()
        {
            if (!typeSerializers.TryGetValue(typeof(T), out Delegate method))
            { throw new NotImplementedException($"Serializer for type {typeof(T)} not found"); }
            return (TypeSerializer<T>)method;
        }

        /// <summary>
        /// Serializes the given <see cref="int"/>
        /// </summary>
        public void Serialize(int v)
        {
            var result = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian) Array.Reverse(result);
            this.result.AddRange(result);
        }
        /// <summary>
        /// Serializes the given <see cref="float"/>
        /// </summary>
        public void Serialize(float v)
        {
            var result = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian) Array.Reverse(result);
            this.result.AddRange(result);
        }
        /// <summary>
        /// Serializes the given <see cref="double"/>
        /// </summary>
        public void Serialize(double v)
        {
            var result = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian) Array.Reverse(result);
            this.result.AddRange(result);
        }
        /// <summary>
        /// Serializes the given <see cref="bool"/>
        /// </summary>
        public void Serialize(bool v)
        {
            result.Add(BitConverter.GetBytes(v)[0]);
        }
        /// <summary>
        /// Serializes the given <see cref="byte"/>
        /// </summary>
        public void Serialize(byte v)
        {
            result.Add(v);
        }
        /// <summary>
        /// Serializes the given <see cref="short"/>
        /// </summary>
        public void Serialize(short v)
        {
            var result = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian) Array.Reverse(result);
            this.result.AddRange(result);
        }
        /// <summary>
        /// Serializes the given <see cref="char"/>
        /// </summary>
        public void Serialize(char v)
        {
            var result = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian) Array.Reverse(result);
            this.result.AddRange(result);
        }
        public void Serialize(ReadableFileFormat.Value v)
        {
            Serialize((int)v.Type);
            switch (v.Type)
            {
                case ReadableFileFormat.ValueType.LITERAL:
                    Serialize(v.String);
                    break;
                case ReadableFileFormat.ValueType.OBJECT:
                    Serialize(v.Dictionary());
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Serializes the given <see cref="string"/>. Both the length and the encoding will be serialized.
        /// </summary>
        public void Serialize(string v)
        {
            if (v == null)
            {
                Serialize(-1);
                return;
            }
            Serialize(v.Length);
            bool isUnicode = false;
            for (int i = 0; i < v.Length; i++)
            {
                if ((ushort)v[i] > byte.MaxValue)
                {
                    isUnicode = true;
                    break;
                }
            }
            Serialize(isUnicode);
            if (isUnicode)
            {
                for (int i = 0; i < v.Length; i++)
                { Serialize(v[i]); }
            }
            else
            {
                for (int i = 0; i < v.Length; i++)
                { Serialize((byte)(ushort)v[i]); }
            }
        }
        /// <summary>
        /// Serializes the given array of <typeparamref name="T"/> with the <see cref="SerializeObject{T}(ISerializable{T})"/> method. The length of the array will also be serialized.
        /// </summary>
        public void SerializeObjects<T>(ISerializable<T>[] v)
        {
            Serialize(v.Length);
            for (int i = 0; i < v.Length; i++)
            { SerializeObject(v[i]); }
        }
        /// <summary>
        /// Serializes the given object <typeparamref name="T"/> with the <see cref="ISerializable{T}.Serialize(Serializer)"/> method.
        /// </summary>
        public void SerializeObject<T>(ISerializable<T> v) => v.Serialize(this);
        /// <summary>
        /// Serializes the given object <typeparamref name="T"/> with the <paramref name="callback"/> function.
        /// </summary>
        public void Serialize<T>(T v, Action<Serializer, T> callback) => callback.Invoke(this, v);
        /// <summary>
        /// Serializes the given array of <typeparamref name="T"/> with the <paramref name="callback"/> function. The length of the array will also be serialized.
        /// </summary>
        public void SerializeArray<T>(T[] v, Action<Serializer, T> callback)
        {
            Serialize(v.Length);
            for (int i = 0; i < v.Length; i++)
            { callback.Invoke(this, v[i]); }
        }
        /// <summary>
        /// Serializes the given value.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Serialize<T>(T v) => GetSerializerForType<T>().Invoke(v);
        public void Serialize<T>(T[] v)
        {
            TypeSerializer<T> method = GetSerializerForType<T>();
            Serialize(v.Length);
            for (int i = 0; i < v.Length; i++)
            { method.Invoke(v[i]); }
        }
        public void Serialize<T>(T[][] v)
        {
            Serialize(v.Length);
            for (int i = 0; i < v.Length; i++)
            { Serialize(v[i]); }
        }
        public void Serialize<T>(T[][][] v)
        {
            Serialize(v.Length);
            for (int i = 0; i < v.Length; i++)
            { Serialize(v[i]); }
        }
        public void Serialize<TKey, TValue>(Dictionary<TKey, TValue> v)
        {
            if (v.Count == 0) { Serialize(-1); return; }

            TypeSerializer<TKey> keySerializer = GetSerializerForType<TKey>();
            TypeSerializer<TValue> valueSerializer = GetSerializerForType<TValue>();

            Serialize(v.Count);

            foreach (var pair in v)
            {
                keySerializer.Invoke(pair.Key);
                valueSerializer.Invoke(pair.Value);
            }
        }
        public void Serialize<TKey, TValue>(Dictionary<TKey, TValue> v, Action<Serializer, TValue> valueSerializer) where TKey : struct
        {
            if (v.Count == 0) { Serialize(-1); return; }

            TypeSerializer<TKey> keySerializer = GetSerializerForType<TKey>();

            Serialize(v.Count);

            foreach (var pair in v)
            {
                keySerializer.Invoke(pair.Key);
                valueSerializer.Invoke(this, pair.Value);
            }
        }
    }
}
