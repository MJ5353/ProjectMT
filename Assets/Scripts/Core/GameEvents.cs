namespace MushTaker.Core
{
    // 게임 전역 이벤트 모음. 새 이벤트는 readonly struct로 여기에 추가한다.

    public readonly struct GameStateChanged
    {
        public readonly GameState From;
        public readonly GameState To;

        public GameStateChanged(GameState from, GameState to)
        {
            From = from;
            To = to;
        }
    }

    public readonly struct InventoryChanged
    {
        public readonly string Id;
        public readonly int Count; // 바뀐 뒤 개수
        public readonly int Delta; // 증감량

        public InventoryChanged(string id, int count, int delta)
        {
            Id = id;
            Count = count;
            Delta = delta;
        }
    }

    public readonly struct FlagChanged
    {
        public readonly string Flag;
        public readonly bool Value;

        public FlagChanged(string flag, bool value)
        {
            Flag = flag;
            Value = value;
        }
    }

    public readonly struct TabChanged
    {
        public readonly int From;
        public readonly int To;

        public TabChanged(int from, int to)
        {
            From = from;
            To = to;
        }
    }
}