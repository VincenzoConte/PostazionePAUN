using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

public class Aes128CounterMode : SymmetricAlgorithm
{
    private readonly ulong _nonce;
    private readonly ulong _counter;
    private readonly AesManaged _aes;

    public Aes128CounterMode(byte[] nonce, ulong counter)
      : this(ConvertNonce(nonce), counter)
    {
    }

    public Aes128CounterMode(ulong nonce, ulong counter)
    {
        _aes = new AesManaged
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };

        _nonce = nonce;
        _counter = counter;
    }

    private static ulong ConvertNonce(byte[] nonce)
    {
        if (nonce == null) throw new ArgumentNullException(nameof(nonce));
        if (nonce.Length < sizeof(ulong)) throw new ArgumentException($"{nameof(nonce)} must have at least {sizeof(ulong)} bytes");

        return BitConverter.ToUInt64(nonce, 0);
    }

    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] ignoredParameter)
    {
        return new CounterModeCryptoTransform(_aes, rgbKey, _nonce, _counter);
    }

    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] ignoredParameter)
    {
        return new CounterModeCryptoTransform(_aes, rgbKey, _nonce, _counter);
    }

    public override void GenerateKey()
    {
        _aes.GenerateKey();
    }

    public override void GenerateIV()
    {
        // IV not needed in Counter Mode
    }
}

public class CounterModeCryptoTransform : ICryptoTransform
{
    private readonly byte[] _nonceAndCounter;
    private readonly ICryptoTransform _counterEncryptor;
    private readonly Queue<byte> _xorMask = new Queue<byte>();
    private readonly SymmetricAlgorithm _symmetricAlgorithm;

    private ulong _counter;

    public CounterModeCryptoTransform(SymmetricAlgorithm symmetricAlgorithm, byte[] key, ulong nonce, ulong counter)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        _symmetricAlgorithm = symmetricAlgorithm ?? throw new ArgumentNullException(nameof(symmetricAlgorithm));
        _counter = counter;
        _nonceAndCounter = new byte[16];
        /*BitConverter.TryWriteBytes(_nonceAndCounter, nonce);
        BitConverter.TryWriteBytes(new Span<byte>(_nonceAndCounter, sizeof(ulong), sizeof(ulong)), counter);*/
        byte[] firstpart = BitConverter.GetBytes(nonce);
        byte[] secondpart = BitConverter.GetBytes(counter).Reverse().ToArray();
        Array.Copy(firstpart, _nonceAndCounter, firstpart.Length);
        Array.Copy(secondpart, 0, _nonceAndCounter, firstpart.Length, secondpart.Length);

        var zeroIv = new byte[_symmetricAlgorithm.BlockSize / 8];
        _counterEncryptor = symmetricAlgorithm.CreateEncryptor(key, zeroIv);
    }

    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
        var output = new byte[inputCount];
        TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
        return output;
    }

    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer,
        int outputOffset)
    {
        for (var i = 0; i < inputCount; i++)
        {
            if (NeedMoreXorMaskBytes())
            {
                EncryptCounterThenIncrement();
            }

            var mask = _xorMask.Dequeue();
            outputBuffer[outputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ mask);
        }

        return inputCount;
    }

    private bool NeedMoreXorMaskBytes()
    {
        return _xorMask.Count == 0;
    }

    private void EncryptCounterThenIncrement()
    {
        var counterModeBlock = new byte[_symmetricAlgorithm.BlockSize / 8];

        _counterEncryptor.TransformBlock(_nonceAndCounter, 0, _nonceAndCounter.Length, counterModeBlock, 0);
        IncrementCounter();

        foreach (var b in counterModeBlock)
        {
            _xorMask.Enqueue(b);
        }
    }

    private void IncrementCounter()
    {
        _counter++;
        byte[] counterBytes = BitConverter.GetBytes(_counter).Reverse().ToArray();
        Array.Copy(counterBytes, 0, _nonceAndCounter, sizeof(ulong), sizeof(ulong));
        /*var span = new Span<byte>(_nonceAndCounter, sizeof(ulong), sizeof(ulong));
        BitConverter.TryWriteBytes(span, _counter);*/
    }

    public int InputBlockSize => _symmetricAlgorithm.BlockSize / 8;
    public int OutputBlockSize => _symmetricAlgorithm.BlockSize / 8;
    public bool CanTransformMultipleBlocks => true;
    public bool CanReuseTransform => false;

    public void Dispose()
    {
        _counterEncryptor.Dispose();
    }
}

//public void ExampleUsage()
//{
//    var key = new byte[16];
//    RandomNumberGenerator.Create().GetBytes(key);
//    var nonce = new byte[8];
//    RandomNumberGenerator.Create().GetBytes(nonce);
//
//    var dataToEncrypt = new byte[12345];
//    var unusedIv = new byte[16];
//
//    var counter = 0;
//    using var counterMode = new Aes128CounterMode(nonce, counter);
//    using var encryptor = counterMode.CreateEncryptor(key, unusedIv);
//    using var decryptor = counterMode.CreateDecryptor(key, unusedIv);
//
//    var encryptedData = new byte[dataToEncrypt.Length];
//    var bytesWritten = encryptor.TransformBlock(dataToEncrypt, 0, dataToEncrypt.Length, encryptedData, 0);
//
//    var decrypted = new byte[dataToEncrypt.Length];
//    decryptor.TransformBlock(encryptedData, 0, bytesWritten, decrypted, 0);
//
//    //decrypted.Should().BeEquivalentTo(dataToEncrypt);
//}

public class AESEncrypter : MonoBehaviour
{
    public static byte[] Encrypt(byte[] toEncrypt, byte[] key)
    {
        byte[] unusedIV = new byte[16];
        Aes128CounterMode counterMode = new Aes128CounterMode(0, 1);
        ICryptoTransform encryptor = counterMode.CreateEncryptor(key, unusedIV);
        byte[] encrypted = new byte[toEncrypt.Length];
        int bytesWritten = encryptor.TransformBlock(toEncrypt, 0, toEncrypt.Length, encrypted, 0);
        if (bytesWritten == encrypted.Length)
            return encrypted;
        byte[] resizedEncrypted = new byte[bytesWritten];
        Array.Copy(encrypted, resizedEncrypted, bytesWritten);
        return resizedEncrypted;
    }

    public static string Encrypt(string toEncrypt, string key)
    {
        return ByteArrayToHexString(Encrypt(Encoding.UTF8.GetBytes(toEncrypt), HexStringToByteArray(key)));
    }

    public static byte[] Decrypt(byte[] toDecrypt, byte[] key)
    {
        byte[] unusedIV = new byte[16];
        Aes128CounterMode counterMode = new Aes128CounterMode(0, 1);       
        ICryptoTransform decryptor = counterMode.CreateDecryptor(key, unusedIV);
        byte[] decrypted = new byte[toDecrypt.Length * 2];
        int bytesWritten = decryptor.TransformBlock(toDecrypt, 0, toDecrypt.Length, decrypted, 0);
        if (bytesWritten == decrypted.Length)
            return decrypted;
        byte[] resizedEncrypted = new byte[bytesWritten];
        Array.Copy(decrypted, resizedEncrypted, bytesWritten);
        return resizedEncrypted;
    }

    public static string Decrypt(string toDecrypt, string key)
    {
        return Encoding.UTF8.GetString(Decrypt(HexStringToByteArray(toDecrypt), HexStringToByteArray(key)));
    }

    public static byte[] HexStringToByteArray(string hex)
    {
        int NumberChars = hex.Length;
        byte[] bytes = new byte[NumberChars / 2];
        for (int i = 0; i < NumberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }

    public static string ByteArrayToHexString(byte[] ba)
    {
        return BitConverter.ToString(ba).Replace("-", "");
    }
}
