using Torch;

namespace LagGridBroadcaster
{
    public class LagGridBroadcasterConfig : ViewModel
    {
        public const string ResultFileDefaultName = "LagGridBroadcasterMeasureResult.xml";
        private ulong _factionMemberDistance = 1000;
        private uint _factionTop = 1;
        private uint _minUs = 500;
        private bool _noOutputWhileEmptyResult = true;
        private string _resultFileName = ResultFileDefaultName;
        private bool _sendResultOfControllingGrid = true;
        private uint _top = 1;
        private bool _writeToFile = true;

        public uint Top
        {
            get => _top;
            set => SetValue(ref _top, value);
        }

        public uint MinUs
        {
            get => _minUs;
            set => SetValue(ref _minUs, value);
        }

        public double MinMs => _minUs / 1000D;

        public ulong FactionMemberDistance
        {
            get => _factionMemberDistance;
            set => SetValue(ref _factionMemberDistance, value);
        }

        public uint FactionTop
        {
            get => _factionTop;
            set => SetValue(ref _factionTop, value);
        }

        public bool SendResultOfControllingGrid
        {
            get => _sendResultOfControllingGrid;
            set => SetValue(ref _sendResultOfControllingGrid, value);
        }

        public bool NoOutputWhileEmptyResult
        {
            get => _noOutputWhileEmptyResult;
            set => SetValue(ref _noOutputWhileEmptyResult, value);
        }

        public bool WriteToFile
        {
            get => _writeToFile;
            set => SetValue(ref _writeToFile, value);
        }

        public string ResultFileName
        {
            get => _resultFileName;
            set => SetValue(ref _resultFileName, value);
        }
    }
}