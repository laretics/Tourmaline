// COPYRIGHT 2010 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TOURMALINE.Common
{
    /// <summary>
    /// Asigna explícitamente el nombre del hilo en que el objetivo va a ejecutarse.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ThreadNameAttribute : Attribute
    {
        readonly string threadName;

        // This is a positional argument
        public ThreadNameAttribute(string threadName)
        {
            this.threadName = threadName;
        }

        public string ThreadName
        {
            get { return threadName; }
        }
    }

    /// <summary>
    /// Define un hilo en el que se permite la ejecución de este destino.
    /// Un solo destino puede permitir varios hilos en los que puede ejecutarse.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class CallOnThreadAttribute : Attribute
    {
        readonly string threadName;

        // Este es un argumento posicional
        public CallOnThreadAttribute(string threadName)
        {
            this.threadName = threadName;
        }

        public string ThreadName
        {
            get { return threadName; }
        }
    }
}
