using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace LetsMeasure15
{
    class VDevice
    {
        public IDevice Device { get; set; }
        public double Distance { get; set; }

        public override bool Equals(object obj)
        {
            return obj is VDevice device &&
                   EqualityComparer<IDevice>.Default.Equals(Device, device.Device);
        }

        public override int GetHashCode()
        {
            return 297346571 + EqualityComparer<IDevice>.Default.GetHashCode(Device);
        }
    }
}
