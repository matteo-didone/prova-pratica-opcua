// SmartBulbOpcUa.Models/SmartBulbPro.cs
using System;

namespace SmartBulbOpcUa.Models
{
    public class SmartBulbPro : SmartBulbBase
    {
        public int Brightness { get; set; } // 0-100
        
        private Random _random = new Random();

        public SmartBulbPro(string id, string name) : base(id, name)
        {
            Brightness = 0;
        }

        public override void TurnOn()
        {
            base.TurnOn();
            if (State == BulbState.ON && Brightness == 0)
            {
                Brightness = 50; // Default brightness quando si accende
            }
        }

        public override void TurnOff()
        {
            base.TurnOff();
            if (State == BulbState.OFF)
            {
                Brightness = 0;
            }
        }

        public void SetBrightness(int level)
        {
            if (State == BulbState.ERROR)
                return;

            if (level < 0 || level > 100)
                throw new ArgumentOutOfRangeException(nameof(level), "Brightness must be between 0 and 100");

            Brightness = level;
            
            // Se si imposta luminosità > 0, accende automaticamente
            if (level > 0 && State == BulbState.OFF)
            {
                State = BulbState.ON;
            }
            // Se si imposta luminosità = 0, spegne
            else if (level == 0 && State == BulbState.ON)
            {
                TurnOff();
            }

            // Aggiorna temperatura in base alla luminosità
            UpdateTemperatureByBrightness();
        }

        private void UpdateTemperatureByBrightness()
        {
            if (State == BulbState.ON)
            {
                // Più è luminosa, più si scalda
                double baseTemp = 20.0 + (Brightness / 100.0) * 25.0; // 20-45°C
                double variation = (_random.NextDouble() - 0.5) * 3.0;
                Temperature = Math.Max(18.0, Math.Min(50.0, baseTemp + variation));
            }
        }
    }
}