// SmartBulbOpcUa.Models/SmartBulbBase.cs
using System;

namespace SmartBulbOpcUa.Models
{
    public class SmartBulbBase
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public BulbState State { get; set; }
        public double Temperature { get; set; }
        
        private Random _random = new Random();

        public SmartBulbBase(string id, string name)
        {
            Id = id;
            Name = name;
            State = BulbState.OFF;
            Temperature = 20.0; // Temperatura ambiente iniziale
        }

        public virtual void TurnOn()
        {
            if (State == BulbState.ERROR)
                return;
                
            State = BulbState.ON;
            // Simula riscaldamento quando si accende
            Temperature = 25.0 + _random.NextDouble() * 15.0; // 25-40°C
        }

        public virtual void TurnOff()
        {
            if (State == BulbState.ERROR)
                return;
                
            State = BulbState.OFF;
            // Simula raffreddamento quando si spegne
            Temperature = 18.0 + _random.NextDouble() * 5.0; // 18-23°C
        }

        public void SetError()
        {
            State = BulbState.ERROR;
            Temperature = _random.NextDouble() * 10.0; // Temperatura anomala
        }

        // Simula variazioni naturali di temperatura
        public void UpdateTemperature()
        {
            if (State == BulbState.ERROR)
                return;

            double variation = (_random.NextDouble() - 0.5) * 2.0; // ±1°C
            
            if (State == BulbState.ON)
            {
                Temperature = Math.Max(25.0, Math.Min(45.0, Temperature + variation));
            }
            else
            {
                Temperature = Math.Max(15.0, Math.Min(25.0, Temperature + variation));
            }
        }
    }
}