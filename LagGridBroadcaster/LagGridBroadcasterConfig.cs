using Torch;

namespace LagGridBroadcaster
{
    public class LagGridBroadcasterConfig : ViewModel
    {
        private uint _top = 3;
        private uint _minUs = 500;
        private ulong _factionMemberDistance = 1000;
        private uint _factionTop = 3;
        private uint _playerMinUs;
        private bool _sendResultOfControllingGrid = true;
        private bool _noOutputWhileEmptyResult = true;
        private bool _writeToFile = true;
        public const string ResultFileDefaultName = "LagGridBroadcasterMeasureResult.xml";
        private string _resultFileName = ResultFileDefaultName;

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

        public uint PlayerMinUs
        {
            get => _playerMinUs;
            set => SetValue(ref _playerMinUs, value);
        }

        public double PlayerMinMs => _playerMinUs / 1000D;

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