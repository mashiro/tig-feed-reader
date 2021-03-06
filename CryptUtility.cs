﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Spica.Applications.TwitterIrcGateway.AddIns.FeedReader
{
	public static class CryptUtility
	{
		public static byte[] Encrypt(SymmetricAlgorithm algo, byte[] src, byte[] key)
		{
			algo.Key = GenerateKey(key, algo.Key.Length);
			algo.IV = GenerateKey(key, algo.IV.Length);
			return Encrypt(algo, src);
		}

		public static byte[] Encrypt(SymmetricAlgorithm algorithm, byte[] src)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				using (CryptoStream cs = new CryptoStream(ms, algorithm.CreateEncryptor(), CryptoStreamMode.Write))
				{
					cs.Write(src, 0, src.Length);
					cs.FlushFinalBlock();
					return ms.ToArray();
				}
			}
		}

		public static byte[] Decrypt(SymmetricAlgorithm algo, byte[] src, byte[] key)
		{
			algo.Key = GenerateKey(key, algo.Key.Length);
			algo.IV = GenerateKey(key, algo.IV.Length);
			return Decrypt(algo, src);
		}

		public static byte[] Decrypt(SymmetricAlgorithm algorithm, byte[] src)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				using (CryptoStream cs = new CryptoStream(ms, algorithm.CreateDecryptor(), CryptoStreamMode.Write))
				{
					cs.Write(src, 0, src.Length);
					cs.FlushFinalBlock();
					return ms.ToArray();
				}
			}
		}

		private static byte[] GenerateKey(byte[] src, int size)
		{
			byte[] dest = new byte[size];
			if (src.Length <= dest.Length)
			{
				for (int i = 0; i < src.Length; ++i)
					dest[i] = src[i];
			}
			else
			{
				int n = 0;
				for (int i = 0; i < src.Length; ++i)
				{
					dest[n++] ^= src[i];
					if (n >= dest.Length) n = 0;
				}
			}

			return dest;
		}
	}
}
