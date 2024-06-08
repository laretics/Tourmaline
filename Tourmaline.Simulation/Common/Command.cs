using Tourmaline.Simulation;
using Tourmaline.Simulation.RollingStocks;
using TOURMALINE.Common;
using System;
using System.Diagnostics;   // Used by Trace.Warnings

namespace Tourmaline.Common
{
    /// <summary>
    /// Esta estructura de comandos permite encapsular las peticiones como objetos (http://sourcemaking.com/design_patterns/command).
    /// El patrón proporciona muchas ventajas, pero permite a Tourmaline guardarlos y salvarlos cuando el usuario pulsa F2.
    /// Es posible leer más tarde estos comandos desde un archivo y reproducirlos.
    /// Los comandos se escriben y se leen usando la serialización binaria de .NET que es rápida de codificar.
    /// Si se quisiera tener una versión editable, se puede intentar con JSON.

    /// Los comandos inmediatos, como por ejemplo el silbato de la máquina, son directos pero continuos (el de aplicar el freno del tren no lo es).
    /// Se intentará que los comandos se puedan repetir con un resultado que tenga la misma precisión independientemente del tipo de hardware.
    /// Los comandos continuos tienen un objetivo (target) que se guarda cuando la tecla se deja de pulsar. Tourmaline crea los comandos
    /// inmediatos en cuanto el usuario pulsa la tecla, pero crea los contínuos una vez que el usuario deja de pulsar la tecla y el destino
    /// es conocido.
    
    /// Todos los comandos almacenan la hora en que fueron creados. Los comandos contínuos se remontan al tiempo en que se pulsó la tecla.

    /// Cada clase "comando" tiene una propiedad "Receiver" que llama a métodos en ese receptor para ejecutar el código asociado.
    /// Esta propiedad es estática por 2 razones:
    /// - Todos los comandos de la misma clase comparten el mismo objeto receptor.
    /// - Al serialiar o deserializar desde un archivo, el receptor no tiene que estar serializado.
    /// 
    /// Hay que asignar el receptor antes de usar cualquier comando.
    /// Por ejemplo:
    ///   ReverserCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
    /// 
    /// </summary>
    public interface ICommand
    {

        /// <summary>
        /// The time when the command was issued (compatible with Simlator.ClockTime).
        /// </summary>
        long Time { get; set; }

        /// <summary>
        /// Call the Receiver to repeat the Command.
        /// Each class of command shares a single object, the Receiver, and the command executes by
        /// call methods of the Receiver.
        /// </summary>
        void Redo();

        /// <summary>
        /// Print the content of the command.
        /// </summary>
        void Report();
    }

    [Serializable()]
    public abstract class Command : ICommand
    {
        public long Time { get; set; }

        /// <summary>
        /// Each command adds itself to the log when it is constructed.
        /// </summary>
        public Command(CommandLog log)
        {
            log.CommandAdd(this as ICommand);
        }

        // Method required by ICommand
        public virtual void Redo() { Trace.TraceWarning("Dummy method"); }

        public override string ToString()
        {
            return this.GetType().ToString();
        }

        // Method required by ICommand
        public virtual void Report()
        {
            Trace.WriteLine(String.Format(
               "Command: {0} {1}",string.Format("{0}",Time), ToString()));
        }
    }

    // <Superclasses>
    [Serializable()]
    public abstract class BooleanCommand : Command
    {
        protected bool ToState;

        public BooleanCommand(CommandLog log, bool toState)
            : base(log)
        {
            ToState = toState;
        }
    }

    [Serializable()]
    public abstract class IndexCommand : Command
    {
        protected int Index;

        public IndexCommand(CommandLog log, int index)
            : base(log)
        {
            Index = index;
        }
    }

    /// <summary>
    /// Superclass for continuous commands. Do not create a continuous command until the operation is complete.
    /// </summary>
    [Serializable()]
    public abstract class ContinuousCommand : BooleanCommand
    {
        protected float? Target;

        public ContinuousCommand(CommandLog log, bool toState, float? target, long startTime)
            : base(log, toState)
        {
            Target = target;
            this.Time = startTime;   // Continuous commands are created at end of change, so overwrite time when command was created
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "increase" : "decrease") + ", target = " + Target.ToString();
        }
    }

    [Serializable()]
    public abstract class PausedCommand : Command
    {
        public double PauseDurationS;

        public PausedCommand(CommandLog log, double pauseDurationS)
            : base(log)
        {
            PauseDurationS = pauseDurationS;
        }

        public override string ToString()
        {
            return String.Format("{0} Paused Duration: {1}", base.ToString(), PauseDurationS);
        }
    }

    [Serializable()]
    public abstract class CameraCommand : Command
    {
        public CameraCommand(CommandLog log)
            : base(log)
        {
        }
    }

    [Serializable()]
    public sealed class SaveCommand : Command
    {
        public string FileStem;

        public SaveCommand(CommandLog log, string fileStem)
            : base(log)
        {
            this.FileStem = fileStem;
            Redo();
        }

        public override void Redo()
        {
            // Redo does nothing as SaveCommand is just a marker and saves the fileStem but is not used during replay to redo the save.
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " to file \"" + FileStem + ".replay\"";
        }
    }
}
