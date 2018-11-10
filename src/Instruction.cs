using System;
using System.Collections.Generic;
using System.Text;

namespace Jabber
{
    public class Instruction
    {
        private string instructions;
        private string setAt;
        private string setBy;

        public Instruction(string instructions, string setBy)
        {
            //TODO Is the person allowed to set instructions?
            if(false)
            {
                //Reject with error message
            }

            this.Instructions = instructions;
            this.SetBy = setBy;
            this.SetAt = DateTime.Now.ToString();

        }

        public string Instructions { get => instructions; set => instructions = value; }
        public string SetAt { get => setAt; set => setAt = value; }
        public string SetBy { get => setBy; set => setBy = value; }

        public override string ToString()
        {
            return string.Format("{0}\n Set By: {1} @ {2}", Instructions, SetBy, SetAt);
        }
    }
}
