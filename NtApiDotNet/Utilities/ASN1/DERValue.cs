﻿//  Copyright 2020 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace NtApiDotNet.Utilities.ASN1
{
    internal struct DERValue
    {
        public DERTagType Type;
        public bool Constructed;
        public int Tag;
        public byte[] Data;
        public DERValue[] Children;
        public long Offset;
        public long DataOffset;

        public bool Check(bool constructed, DERTagType type, int tag)
        {
            return Constructed == constructed && Type == type && Tag == tag;
        }

        public bool CheckApplication(int tag)
        {
            return Check(true, DERTagType.Application, tag);
        }

        public bool CheckPrimitive(UniversalTag tag)
        {
            return Check(false, DERTagType.Universal, (int)tag);
        }

        public bool HasChildren()
        {
            return (Children?.Length ?? 0) != 0;
        }

        public string FormatTag()
        {
            if (Type == DERTagType.Universal)
                return ((UniversalTag)Tag).ToString();
            return Tag.ToString();
        }

        private static IEnumerable<bool> GetBool(byte b)
        {
            bool[] ret = new bool[8];
            for (int i = 0; i < 8; ++i)
            {
                ret[i] = ((b >> (7 - i)) & 1) != 0;
            }
            return ret;
        }

        public BitArray ReadBitString()
        {
            if (Data.Length == 0)
                return new BitArray(0);
            IEnumerable<bool> bools = Data.Skip(1).SelectMany(b => GetBool(b));
            int total_count = (Data.Length - 1) * 8 - Data[0];
            return new BitArray(bools.Take(total_count).ToArray());
        }

        public string ReadObjID()
        {
            List<int> values = new List<int>();
            BinaryReader reader = new BinaryReader(new MemoryStream(Data));
            byte first = reader.ReadByte();
            values.Add(first / 40);
            values.Add(first % 40);
            while (reader.RemainingLength() > 0)
            {
                values.Add(reader.ReadEncodedInt());
            }
            return string.Join(".", values);
        }

        public BigInteger ReadInteger()
        {
            return new BigInteger(Data.Reverse().ToArray());
        }

        private string FormatInteger()
        {
            return ReadInteger().ToString("X");
        }

        public string ReadGeneralString()
        {
            return Encoding.ASCII.GetString(Data);
        }

        public string FormatValue()
        {
            if (Type == DERTagType.Universal)
            {
                UniversalTag tag = (UniversalTag)Tag;
                if (tag == UniversalTag.GeneralString)
                    return ReadGeneralString();
                if (tag == UniversalTag.OBJECT_IDENTIFIER)
                    return ReadObjID();
                if (tag == UniversalTag.INTEGER)
                    return FormatInteger();
                if (tag == UniversalTag.OCTET_STRING)
                    return BitConverter.ToString(Data);
                if (tag == UniversalTag.BIT_STRING)
                    return string.Join(",", ReadBitString().Cast<bool>().Select(b => b ? 1 : 0));
            }
            return $"Len: {Data.Length:X}";
        }
    }
}