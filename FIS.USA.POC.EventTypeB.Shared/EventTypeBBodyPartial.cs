using System;
using System.Collections.Generic;
using System.Text;

namespace FIS.USA.POC.EventTypeB.Shared
{
    /// <summary>
    /// This method is in a separate partial class file so it is not overwritten when you use avrogen
    /// </summary>
    public partial class EventTypeBBody
    {
        public override string ToString()
        {
            return $"id: {id}, name: {name}, favorite_number: {favorite_number}, favorite_color: {favorite_color}";
        }
    }
}
