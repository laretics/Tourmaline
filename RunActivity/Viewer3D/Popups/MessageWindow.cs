using Microsoft.Xna.Framework;
using TOURMALINE.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
namespace Tourmaline.Viewer3D.Popups
{
    public class MessageWindow:LayeredWindow
    {
        const int HorizontalPadding = 160; //Más ancha que el monitor de vía.
        const int VerticalPadding = 150; //Más alta que la próxima estación.
        const int TextSize = 18;
        float FadeTime { get; set; }

        List<Message> messages = new List<Message>();
        bool messagesChanged;

        public MessageWindow(WindowManager owner)
            : base(owner,HorizontalPadding,VerticalPadding,"Mensajes")
        {
            FadeTime = 2000;
            Visible = true;
        }

        protected override void LocationChanged()
        {
            //SizeTo no ajusta el tamaño. Hay que hacerlo antes. MoveTo sí que lo ajusta.
            SizeTo(Owner.ScreenSize.X-2*HorizontalPadding,Owner.ScreenSize.Y-2*VerticalPadding);
            MoveTo(HorizontalPadding, VerticalPadding);
            base.LocationChanged();
        }

        public override bool Interactive => false;

        public override bool TopMost => true;

        protected override ControlLayout Layout(ControlLayout layout)
        {
            ControlLayoutVertical vBox = base.Layout(layout).AddLayoutVertical();

            int maxLines = vBox.RemainingHeight / TextSize;
            List<Message> messages = this.messages.Take(maxLines).Reverse().ToList();
            vBox.AddSpace(0, vBox.RemainingHeight - TextSize * messages.Count);
            foreach(Message msg in messages)
            {
                ControlLayoutHorizontal hBox = vBox.AddLayoutHorizontal(TextSize);
                int width= Owner.Viewer.WindowManager.TextFontDefault.MeasureString(msg.Text);
                hBox.Add(msg.LabelShadow = new LabelShadow(width, hBox.RemainingHeight));
                hBox.Add(msg.LabelText = new Label(-width, 0, width, TextSize, msg.Text));
            }
            return vBox;
        }

        long lastElapsedTime;
        public override void PrepareFrame(long elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);
            lastElapsedTime = elapsedTime;
            if (messagesChanged)
            {
                messagesChanged = false;
                Layout();
            }
            foreach (Message msg in messages)
            {
                msg.TimeToShow -= elapsedTime;
                if(msg.TimeToShow<FadeTime)
                {
                    float resta = FadeTime - msg.TimeToShow;                    
                    float valorClamp = MathHelper.Clamp(resta / FadeTime, 0, 1);
                    msg.LabelShadow.Color.A = msg.LabelText.Color.A = (byte)MathHelper.Lerp(255, 0, valorClamp);
                }                            
            }                
        }

        public void AddMessage(string Key, string text, long duration)
        {
            while (true)
            {
                //Almacena la lista original y hace un clon para reemplazarla con seguridad en multiproceso.
                List<Message> oldMessages = messages;
                List<Message> newMessages = new List<Message>(oldMessages);

                //Elimina cualquier mensaje con una clave duplicada o los que han caducado.
                newMessages=(from m in newMessages
                             where(String.IsNullOrEmpty(Key) || !m.Key.Equals(Key))
                             && m.TimeToShow>0
                             select m).ToList();

                //Busca algún mensaje con la misma clave, suponiendo que lo hubiera ya.
                Message existingMessage = String.IsNullOrEmpty(Key) ? null : newMessages.FirstOrDefault(m => m.Key.Equals(Key));

                if (null != existingMessage)
                    existingMessage.TimeToShow += duration;
                else
                    newMessages.Add(new Message(Key, string.Format("{0} {1}", DateTime.Now,text),duration + FadeTime));

                //Ordena los mensajes de la nueva lista
                newMessages=(from m in newMessages
                             orderby m.TimeToShow descending
                             select m).ToList();

                //Seguridad de subprocesos: Comparamos la nueva lista con la vieja. Sólo hay éxito si el valor de retorno coincide con la lista vieja.
                if (Interlocked.CompareExchange(ref messages, newMessages, oldMessages) == oldMessages) break;
             }
            messagesChanged = true;
        }

        public void AddMessage(string text, long duration)
        {
            AddMessage(string.Empty, text, duration);
        }

        class Message
        {
            public readonly string Key;
            public readonly string Text;
            public float TimeToShow { get; set; }
            internal LabelShadow LabelShadow;
            internal Label LabelText;

            public Message(string key, string text, float timeToShow)
            {
                this.Key = key;
                this.Text = text;
                this.TimeToShow = timeToShow;
            }
            public Message(BinaryReader inf)
            {
                Key = inf.ReadString();
                Text = inf.ReadString();
            }
        }
    }



}
