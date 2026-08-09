using System;
using System.Collections.Generic;

namespace JxModule
{
    public static class SaveSlotSystem
    {
        public const int MaxSlotCount = 3;

        private const string SaveDirectory = "Save";
        private const string FileExtension = ".save";

        public static void Save<T>(int slot, T data, bool prettyPrint = false) where T : ISaveSlotData
        {
            ValidateSlot(slot);

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.SlotInfo == null)
            {
                throw new InvalidOperationException($"{typeof(T).Name}.SlotInfo is null.");
            }

            data.SlotInfo.slot = slot;
            data.SlotInfo.UpdateSavedAt();

            SaveSystem.Save(data, GetFileName(slot), SaveDirectory, prettyPrint);
        }

        public static T Load<T>(int slot) where T : ISaveSlotData
        {
            ValidateSlot(slot);
            return SaveSystem.Load<T>(GetFileName(slot), SaveDirectory);
        }

        public static bool TryLoad<T>(int slot, out T data) where T : ISaveSlotData
        {
            ValidateSlot(slot);
            return SaveSystem.TryLoad(GetFileName(slot), SaveDirectory, out data);
        }

        public static bool Exist(int slot)
        {
            ValidateSlot(slot);
            return SaveSystem.Exist(GetFileName(slot), SaveDirectory);
        }

        public static void Remove(int slot)
        {
            ValidateSlot(slot);
            SaveSystem.Remove(GetFileName(slot), SaveDirectory);
        }

        public static void RemoveAll()
        {
            SaveSystem.RemoveAll(SaveDirectory);
        }

        public static IEnumerable<int> GetSlots()
        {
            for (var slot = 0; slot < MaxSlotCount; slot++)
            {
                yield return slot;
            }
        }

        public static IEnumerable<int> GetExistingSlots()
        {
            for (var slot = 0; slot < MaxSlotCount; slot++)
            {
                if (Exist(slot))
                {
                    yield return slot;
                }
            }
        }

        private static string GetFileName(int slot)
        {
            return $"Slot_{slot}{FileExtension}";
        }

        private static void ValidateSlot(int slot)
        {
            if (slot < 0 || slot >= MaxSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, $"Slot must be between 0 and {MaxSlotCount - 1}.");
            }
        }
    }
}