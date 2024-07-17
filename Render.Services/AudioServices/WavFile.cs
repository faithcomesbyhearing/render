namespace Render.Services.AudioServices
{
    public class WaveFile {
        
        /// <summary>
        /// The Riff header is 12 bytes long
        /// </summary>
        public class RiffBlock {

            public RiffBlock() {
                RiffId = new byte[4];
                RiffFormat = new byte[4];
            }

            public void ReadRiff(MemoryStream memoryStream) {
                try
                {
                    memoryStream.Read(RiffId, 0, 4);
                    //Debug.Assert(_RiffID[0] == 82, "Riff ID Not Valid");
                    var binRead = new BinaryReader(memoryStream);
                    _riffSize = binRead.ReadUInt32();

                    memoryStream.Read(RiffFormat, 0, 4);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            public byte[] RiffId { get; private set; }

            public uint RiffSize {
                get { return (_riffSize); }
            }

            public byte[] RiffFormat { get; private set; }

            private uint _riffSize;
        }

        /// <summary>
        /// The Format header is 24 bytes long
        /// </summary>
        public class FmtBlock {
            public FmtBlock() {
                FmtId = new byte[4];
            }

            public void ReadFmt(MemoryStream memoryStream) {
                try
                {
                    memoryStream.Read(FmtId, 0, 4);

                    //Debug.Assert(_FmtID[0] == 102, "Format ID Not Valid");

                    var binRead = new BinaryReader(memoryStream);

                    FmtSize = binRead.ReadUInt32();
                    FmtTag = binRead.ReadUInt16();
                    Channels = binRead.ReadUInt16();
                    SamplesPerSec = (int)binRead.ReadUInt32() * Channels;
                    AverageBytesPerSec = (int)binRead.ReadUInt32();
                    BlockAlign = binRead.ReadUInt16();
                    BitsPerSample = binRead.ReadUInt16();

                    // This accounts for the variable format header size 
                    // 12 bytes of Riff Header, 4 bytes for FormatId, 4 bytes for FormatSize & the Actual size of the Format Header 
                    memoryStream.Seek(FmtSize + 20, SeekOrigin.Begin);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            public byte[] FmtId { get; private set; }

            public uint FmtSize { get; private set; }

            public ushort FmtTag { get; private set; }

            public ushort Channels { get; private set; }

            public int SamplesPerSec { get; private set; }

            public int AverageBytesPerSec { get; private set; }

            public ushort BlockAlign { get; private set; }

            public ushort BitsPerSample { get; private set; }
        }

        /// <summary>
        /// The Data block is 8 bytes + ???? long
        /// </summary>
        public class DataBlock {
            public DataBlock() {
                DataId = new byte[4];
            }

            public void ReadData(MemoryStream memoryStream) {
                try
                {
                    memoryStream.Read(DataId, 0, 4);
                    //Debug.Assert(_DataID[0] == 100, "Data ID Not Valid");
                    var binRead = new BinaryReader(memoryStream);
                    NumSamples = (int)binRead.ReadUInt32() / 2;
                    _data = new Int16[NumSamples];
                    memoryStream.Seek(40, SeekOrigin.Begin);
                    
                    for (var i = 0; i < NumSamples; i++)
                    {
                        _data[i] = binRead.ReadInt16();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            public byte[] DataId { get; private set; }
            
            public Int16 this[int pos] {
                get { return _data[pos]; }
            }

            public int NumSamples { get; private set; }

            private Int16[] _data;

            public void Deallocate() {
                _data = null;
            }
        }

        public WaveFile(MemoryStream memoryStream)
        {
            _memoryStream = memoryStream;

            _riff = new RiffBlock();
            _fmt = new FmtBlock();
            Data = new DataBlock();
        }

        public void Read() {
            _riff.ReadRiff(_memoryStream);
            _fmt.ReadFmt(_memoryStream);
            Data.ReadData(_memoryStream);
            _memoryStream.Close();
        }

        public DataBlock Data { get; private set; }

        public FmtBlock Format {
            get { return _fmt; }
        }

        public RiffBlock Riff {
            get { return _riff; }
        }

        private readonly MemoryStream _memoryStream;

        private readonly RiffBlock _riff;
        private readonly FmtBlock _fmt;

        public void Deallocate() {
            Data.Deallocate();
            Data = null;
        }
    }
}