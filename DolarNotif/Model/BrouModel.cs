using System;
using System.Collections.Generic;
using System.Text;

namespace DolarNotif.Model
{
    public class Moneda
    {
        public double Compra { get; set; }
        public double Venta { get; set; }
    }


    public class BrouModel
    {
        public Moneda DolarEBrou { get; set; }
        public Moneda Dolar { get; set; }

        public BrouModel()
        {
            Dolar = new Moneda();
            DolarEBrou = new Moneda();
        }
    }
}
