using System;
using System.Collections.Generic;
using System.IO;
using ManagedLZO;

namespace UnHood.Engine.UE3
{
    class ChunkedStream: Stream
    {
        private readonly Stream _baseStream;
        const int HEADER_MAGIC = -1641380927;

        private class Chunk
        {
            private readonly int _offset;
            private readonly int _size;
            private readonly int _compressedOffset;
            private readonly int _compressedSize;
            private int _blockSize;
            private int _curBlockIndex = -1;
            private byte[] _curBlockData;
            private readonly List<int> _compressedBlockSizes = new List<int>();
            private readonly List<int> _uncompressedBlockSizes = new List<int>();
            private readonly List<int> _blockStarts = new List<int>();

            public Chunk(int offset, int size, int compressedOffset, int compressedSize)
            {
                _offset = offset;
                _size = size;
                _compressedSize = compressedSize;
                _compressedOffset = compressedOffset;
            }

            internal bool Contains(long position)
            {
                return position >= _offset && position < _offset + _size;
            }

            internal void LoadBlocks(Stream stream)
            {
                if (_compressedBlockSizes.Count > 0) return;
                stream.Position = _compressedOffset;
                var r = new BinaryReader(stream);
                if (r.ReadInt32() != HEADER_MAGIC)
                    throw new InvalidDataException("Chunk signature not found");
                _blockSize = r.ReadInt32();
                r.ReadInt32();  // compressed size
                var usize = r.ReadInt32();
                if (usize != _size)
                    throw new InvalidDataException("Uncompressed size mismatch");
                var blockCount = (_size%_blockSize == 0) ? _size/_blockSize : _size/_blockSize + 1;
                int blockStart = (int) stream.Position + blockCount * 8;
                for (int i = 0; i < blockCount; i++)
                {
                    var compressedSize = r.ReadInt32();
                    _compressedBlockSizes.Add(compressedSize);
                    _uncompressedBlockSizes.Add(r.ReadInt32());
                    _blockStarts.Add(blockStart);
                    blockStart += compressedSize;
                }
            }

            internal int Read(Stream s, long offsetInFile, byte[] buffer, int offset, int count)
            {
                int offsetInChunk = (int) (offsetInFile - _offset);
                int bytesRead = 0;
                int blockIndex = offsetInChunk / _blockSize;
                while (offsetInChunk < _size)
                {
                    if (blockIndex != _curBlockIndex)
                        ReadBlock(s, blockIndex);
                    int offsetInBlock = offsetInChunk % _blockSize;
                    int sizeInBlock = Math.Min(count-bytesRead, _uncompressedBlockSizes[blockIndex] - offsetInBlock);
                    Array.ConstrainedCopy(_curBlockData, offsetInBlock, buffer, offset, sizeInBlock);
                    bytesRead += sizeInBlock;
                    if (bytesRead == count) break;
                    offset += sizeInBlock;
                    offsetInChunk += sizeInBlock;
                    blockIndex++;
                }
                return bytesRead;
            }

            private void ReadBlock(Stream s, int index)
            {
                var compSize = _compressedBlockSizes[index];
                byte[] compressedData = new byte[compSize];
                s.Position = _blockStarts[index];
                s.Read(compressedData, 0, compSize);
                _curBlockData = new byte[_uncompressedBlockSizes[index]];
                _curBlockIndex = index;
                MiniLZO.Decompress(compressedData, _curBlockData);
            }
        }

        private readonly List<Chunk> _chunks = new List<Chunk>();
        private Chunk _curChunk;

        public ChunkedStream(Stream baseStream)
        {
            _baseStream = baseStream;
        }

        public void AddChunk(int offset, int size, int compressedOffset, int compressedSize)
        {
            _chunks.Add(new Chunk(offset, size, compressedOffset, compressedSize));
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    throw new NotImplementedException();
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            while(bytesRead < count)
            {
                LoadChunk(Position);
                bytesRead += _curChunk.Read(_baseStream, Position, buffer, offset, count);
                Position += bytesRead;
                offset += bytesRead;
                count -= bytesRead;
            }
            return bytesRead;
        }

        private void LoadChunk(long position)
        {
            if (_curChunk != null && _curChunk.Contains(position))
                return;
            _curChunk = _chunks.Find(c => c.Contains(position));
            _curChunk.LoadBlocks(_baseStream);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new System.NotImplementedException(); }
        }

        public override long Position { get; set; }
    }
}
