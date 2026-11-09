using System.IO;
using ColossalFramework.IO;
using ICities;

namespace PolitiCities.Politics
{
    public class PoliticalData : SerializableDataExtensionBase
    {
        public static PoliticalState Current;

        private static string SaveFile
        {
            get { return Path.Combine(DataLocation.localApplicationData, "PolitiCities.dat"); }
        }

        public override void OnCreated(ISerializableData serializableData)
        {
            Current = new PoliticalState();
        }

        public override void OnLoadData()
        {
            if (File.Exists(SaveFile))
            {
                try
                {
                    using (FileStream fs = File.OpenRead(SaveFile))
                    using (BinaryReader r = new BinaryReader(fs))
                    {
                        var state = new PoliticalState();
                        state.Read(r);
                        Current = state;
                    }
                }
                catch (IOException)
                {
                    Current = new PoliticalState();
                }
            }
        }

        public override void OnSaveData()
        {
            if (Current == null)
            {
                return;
            }
            using (FileStream fs = File.Create(SaveFile))
            using (BinaryWriter w = new BinaryWriter(fs))
            {
                Current.Write(w);
            }
        }

        public override void OnReleased()
        {
            Current = null;
        }
    }
}
