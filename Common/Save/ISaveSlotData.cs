namespace JxModule
{
    public interface ISaveSlotData : ISerializeData
    {
        SaveSlotInfo SlotInfo { get; }
    }
}