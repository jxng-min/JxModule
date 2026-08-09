using System;

namespace JxModule
{
    [Serializable]
    public class SaveSlotInfo : ISerializeData
    {
        public int slot;
        public long savedAt;
        public long playTime;

        public DateTimeOffset SavedDateTime => DateTimeOffset.FromUnixTimeSeconds(savedAt);
        public DateTimeOffset LocalSavedDateTime => SavedDateTime.ToLocalTime();

        public void UpdateSavedAt()
        {
            savedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}