﻿using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Renci.SshNet.Sftp;

namespace Renci.SshNet.Tests.Classes.Sftp
{
    [TestClass]
    public class SftpFileStreamTest_Read_ReadMode_NoDataInReaderBufferAndReadMoreBytesThanCount : SftpFileStreamTestBase
    {
        private string _path;
        private SftpFileStream _target;
        private byte[] _handle;
        private uint _bufferSize;
        private uint _readBufferSize;
        private uint _writeBufferSize;
        private int _actual;
        private byte[] _buffer;
        private byte[] _serverData;
        private int _numberOfBytesInReadBuffer;
        private int _numberOfBytesToRead;

        protected override void SetupData()
        {
            base.SetupData();

            var random = new Random();
            _path = random.Next().ToString();
            _handle = GenerateRandom(5, random);
            _bufferSize = (uint) random.Next(1, 1000);
            _readBufferSize = 20;
            _writeBufferSize = 500;

            _numberOfBytesToRead = 20;
            _buffer = new byte[_numberOfBytesToRead];
            _numberOfBytesInReadBuffer = 10;
            _serverData = GenerateRandom(_buffer.Length + _numberOfBytesInReadBuffer, random);
        }

        protected override void SetupMocks()
        {
            SftpSessionMock.InSequence(MockSequence)
                .Setup(p => p.RequestOpen(_path, Flags.Read, false))
                .Returns(_handle);
            SftpSessionMock.InSequence(MockSequence)
                .Setup(p => p.CalculateOptimalReadLength(_bufferSize))
                .Returns(_readBufferSize);
            SftpSessionMock.InSequence(MockSequence)
                .Setup(p => p.CalculateOptimalWriteLength(_bufferSize, _handle))
                .Returns(_writeBufferSize);
            SftpSessionMock.InSequence(MockSequence)
                .Setup(p => p.IsOpen)
                .Returns(true);
            SftpSessionMock.InSequence(MockSequence)
                .Setup(p => p.RequestRead(_handle, 0UL, _readBufferSize))
                .Returns(_serverData);
        }

        [TestCleanup]
        public void TearDown()
        {
            SftpSessionMock.InSequence(MockSequence)
                           .Setup(p => p.RequestClose(_handle));
        }

        protected override void Arrange()
        {
            base.Arrange();

            _target = new SftpFileStream(SftpSessionMock.Object,
                                         _path,
                                         FileMode.Open,
                                         FileAccess.Read,
                                         (int) _bufferSize);
        }

        protected override void Act()
        {
            _actual = _target.Read(_buffer, 0, _numberOfBytesToRead);
        }

        [TestMethod]
        public void ReadShouldHaveReturnedTheNumberOfBytesWrittenToBuffer()
        {
            Assert.AreEqual(_buffer.Length, _actual);
        }

        [TestMethod]
        public void ReadShouldHaveWrittenBytesToTheCallerSuppliedBuffer()
        {
            Assert.IsTrue(_serverData.Take(_buffer.Length).IsEqualTo(_buffer));
        }

        [TestMethod]
        public void PositionShouldReturnNumberOfBytesWrittenToBuffer()
        {
            SftpSessionMock.InSequence(MockSequence).Setup(p => p.IsOpen).Returns(true);

            Assert.AreEqual(_buffer.Length, _target.Position);

            SftpSessionMock.Verify(p => p.IsOpen, Times.Exactly(2));
        }

        [TestMethod]
        public void ReadShouldReturnAllRemaningBytesFromReadBufferWhenCountIsEqualToNumberOfRemainingBytes()
        {
            SftpSessionMock.InSequence(MockSequence).Setup(p => p.IsOpen).Returns(true);

            _buffer = new byte[_numberOfBytesInReadBuffer];

            var actual = _target.Read(_buffer, 0, _numberOfBytesInReadBuffer);

            Assert.AreEqual(_numberOfBytesInReadBuffer, actual);
            Assert.IsTrue(_serverData.Take(_numberOfBytesToRead, _numberOfBytesInReadBuffer).IsEqualTo(_buffer));

            SftpSessionMock.Verify(p => p.IsOpen, Times.Exactly(2));
        }
    }
}
