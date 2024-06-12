using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Tourmaline.Common;
using Tourmaline.Simulation.RollingStocks;
using TOURMALINE.Common;
using TOURMALINE.Common.Input;

namespace Tourmaline.Viewer3D.RollingStock
{
    public class WagonViewer:TrainCarViewer
    {
        protected PoseableShape TrainCarShape;
        protected AnimatedShape InteriorShape;

        //Rotamos las ruedas a mano y no en el archivo shape
        float WheelRotationR;
        List<int> WheelPartIndexes = new List<int>();

        //El resto de animaciones vienen en el archivo shape
        AnimatedPart runningGear;
        AnimatedPart pantograph1;
        AnimatedPart pantograph2;
        AnimatedPart pantograph3;
        AnimatedPart pantograph4;
        AnimatedPart leftDoor;
        AnimatedPart rightDoor;
        AnimatedPart mirrors;
        protected AnimatedPart wipers;
        protected AnimatedPart bell;
        AnimatedPart unloadingParts;

        bool hasFirstPanto;
        int numBogie1, numBogie2, bogie1Axles, bogie2Axles = 0;
        int bogieMatrix1, bogieMatrix2 = 0;
        
        public WagonViewer(Viewer viewer,MSTSWagon car,WorldPosition position)
            :base(viewer,car)
        {
            string wagonFolderSlash = Path.GetDirectoryName(car.WagFilePath) + @"\";
            TrainCarShape = car.MainShapeFileName != string.Empty
                ? new PoseableShape(viewer, wagonFolderSlash + car.MainShapeFileName + '\0' + wagonFolderSlash, position, ShapeFlags.ShadowCaster)
                : new PoseableShape(viewer, null, position);
            if (null != car.InteriorShapeFileName)
                InteriorShape = new AnimatedShape(viewer, wagonFolderSlash + car.InteriorShapeFileName + '\0' + wagonFolderSlash, position, ShapeFlags.Interior, 30.0f);

            runningGear = new AnimatedPart(TrainCarShape);
            pantograph1 = new AnimatedPart(TrainCarShape);
            pantograph2 = new AnimatedPart(TrainCarShape);
            pantograph3 = new AnimatedPart(TrainCarShape);
            pantograph4 = new AnimatedPart(TrainCarShape);
            leftDoor = new AnimatedPart(TrainCarShape);
            rightDoor = new AnimatedPart(TrainCarShape);
            mirrors = new AnimatedPart(TrainCarShape);
            wipers = new AnimatedPart(TrainCarShape);

            //Determina si tiene el primer pantógrafo.
            //Si lo tiene podemos asociar los pantógrafos sin numerar correctamente
            for(int i=0;i<TrainCarShape.Hierarchy.Length;i++)
                if (TrainCarShape.SharedShape.MatrixNames[i].Contains('1'))
                {
                    if (TrainCarShape.SharedShape.MatrixNames[i].ToUpper().StartsWith("PANTO")) { hasFirstPanto = true; break; }
                }

            //Comprueba los bogies y las ruedas.
            for(int i=0;i<TrainCarShape.Hierarchy.Length;i++)
            {
                if (TrainCarShape.SharedShape.MatrixNames[i].Equals("BOGIE1"))
                {
                    bogieMatrix1 = i;
                    numBogie1 += 1;
                }
                if (TrainCarShape.SharedShape.MatrixNames[i].Equals("BOGIE2"))
                {
                    bogieMatrix2 = i;
                    numBogie2 += 1;
                }
                if (TrainCarShape.SharedShape.MatrixNames[i].Equals("BOGIE"))
                {
                    bogieMatrix1 = i;
                }
                //De momento, el número total de ejes será la suma de los ejes del bogie que está contando
                if (TrainCarShape.SharedShape.MatrixNames[i].Contains("WHEELS"))
                {
                    if (8 == TrainCarShape.SharedShape.MatrixNames[i].Length)
                    {
                        int tpmatrix = TrainCarShape.SharedShape.GetParentMatrix(i);
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS11") && tpmatrix == bogieMatrix1)
                            bogie1Axles++;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS12") && tpmatrix == bogieMatrix1)
                            bogie1Axles++;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS13") && tpmatrix == bogieMatrix1)
                            bogie1Axles++;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS21") && tpmatrix == bogieMatrix1)
                            bogie1Axles++;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS22") && tpmatrix == bogieMatrix1)
                            bogie1Axles++;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS23") && tpmatrix == bogieMatrix1)
                            bogie1Axles++;

                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS11") && tpmatrix == bogieMatrix2)
                            bogie2Axles++;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS12") && tpmatrix == bogieMatrix2)
                            bogie2Axles++;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS13") && tpmatrix == bogieMatrix2)
                            bogie2Axles++;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS21") && tpmatrix == bogieMatrix2)
                            bogie2Axles++;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS22") && tpmatrix == bogieMatrix2)
                            bogie2Axles++;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS23") && tpmatrix == bogieMatrix2)
                            bogie2Axles++;
                    }
                }
            }

            // Relaciona todas las matrices con sus respectivas partes
            for (int i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                if (-1 == TrainCarShape.Hierarchy[i])
                    MatchMatrixToPart(car, i, 0);

            car.SetUpWheels();           
        }

        private void MatchMatrixToPart(MSTSWagon car, int matrix,int bogieMatrix)
        {
            string matrixName = TrainCarShape.SharedShape.MatrixNames[matrix].ToUpper();

            bool matrixAnimated = null!=TrainCarShape.SharedShape.Animations 
                && TrainCarShape.SharedShape.Animations.Count>0
                && TrainCarShape.SharedShape.Animations[0].anim_nodes.Count > matrix
                && TrainCarShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 0;

            if(matrixName.StartsWith("WHEELS") && (7==matrixName.Length || 8==matrixName.Length || 9==matrixName.Length))
            {
                //La longitud de las ruedas standard debería ser 8 para probar en WHEELS11.
                Matrix m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                //Algunos creadores usan las ruedas para animar ventiladoes. Hay que comprobar que la rueda no esté demasiado arriba (que sea por debajo
                //de 3 metros), para animarla como una rueda real.
                if (m.M42 < 3)
                {
                    int id = 0;
                    //Ñapa porque los creadores de modelos no siguen las reglas estándar
                    int tmatrix = TrainCarShape.SharedShape.GetParentMatrix(matrix);
                    if (8 == matrixName.Length && 0 == bogieMatrix && tmatrix == 0) //Ruedas sueltas que no forman parte de un bogie
                        matrixName = TrainCarShape.SharedShape.MatrixNames[matrix].Substring(0, 7); //Cambiamos el nombre de la rueda 
                    if (8 == matrixName.Length || 9 == matrixName.Length)
                        Int32.TryParse(matrixName.Substring(6, 1), out id);
                    if (8 == matrixName.Length || 9 == matrixName.Length || !matrixAnimated)
                        WheelPartIndexes.Add(matrix);
                    else
                        runningGear.AddMatrix(matrix);

                    int pmatrix = TrainCarShape.SharedShape.GetParentMatrix(matrix);
                    car.AddWheelSet(m.M43, id, pmatrix, matrixName.ToString(), bogie1Axles, bogie2Axles);
                }
                else //Las ruedas standard son procesadas arriba, pero las ruedas que se usan como ventiladores van aquí abajo.
                    runningGear.AddMatrix(matrix);
            }
            else if(matrixName.StartsWith("BOGIE") && matrixName.Length<=6) //Vale BOGIE1, pero no BOGIE11
            {
                if(6==matrixName.Length)
                {
                    int id = 1;
                    Int32.TryParse(matrixName.Substring(5), out id);
                    Matrix m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                    car.AddBogie(m.M43, matrix, id, matrixName.ToString(), numBogie1, numBogie2);
                    bogieMatrix = matrix; //Tenemos que salvar la matriz del bogie para probar con los ejes.
                }
                else
                {
                    int id = 1;
                    Matrix m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                    car.AddBogie(m.M43,matrix,id,matrixName.ToString(),numBogie1, numBogie2);
                    bogieMatrix = matrix; //Tenemos que salvar la matriz del bogie para probar con los ejes.
                }
                //Los bogies contienen ruedas
                for (int i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                    if (TrainCarShape.Hierarchy[i] == matrix)
                        MatchMatrixToPart(car, i, bogieMatrix);
            }
            else if (matrixName.StartsWith("WIPER")) //Limpias
            {
                wipers.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("DOOR")) //Izquierdas y derechas
            {
                if (matrixName.StartsWith("DOOR_D") || matrixName.StartsWith("DOOR_E") || matrixName.StartsWith("DOOR_F"))
                    leftDoor.AddMatrix(matrix);
                else if (matrixName.StartsWith("DOOR_A") || matrixName.StartsWith("DOOR_B") || matrixName.StartsWith("DOOR_C"))
                    rightDoor.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("PANTOGRAPH"))
            {
                switch (matrixName)
                {
                    case "PANTOGRAPHBOTTOM1":
                    case "PANTOGRAPHBOTTOM1A":
                    case "PANTOGRAPHBOTTOM1B":
                    case "PANTOGRAPHMIDDLE1":
                    case "PANTOGRAPHMIDDLE1A":
                    case "PANTOGRAPHMIDDLE1B":
                    case "PANTOGRAPHTOP1":
                    case "PANTOGRAPHTOP1A":
                    case "PANTOGRAPHTOP1B":
                        pantograph1.AddMatrix(matrix);
                        break;
                    case "PANTOGRAPHBOTTOM2":
                    case "PANTOGRAPHBOTTOM2A":
                    case "PANTOGRAPHBOTTOM2B":
                    case "PANTOGRAPHMIDDLE2":
                    case "PANTOGRAPHMIDDLE2A":
                    case "PANTOGRAPHMIDDLE2B":
                    case "PANTOGRAPHTOP2":
                    case "PANTOGRAPHTOP2A":
                    case "PANTOGRAPHTOP2B":
                        pantograph2.AddMatrix(matrix);
                        break;
                    default://someone used other language
                        if (matrixName.Contains("1"))
                            pantograph1.AddMatrix(matrix);
                        else if (matrixName.Contains("2"))
                            pantograph2.AddMatrix(matrix);
                        else if (matrixName.Contains("3"))
                            pantograph3.AddMatrix(matrix);
                        else if (matrixName.Contains("4"))
                            pantograph4.AddMatrix(matrix);
                        else
                        {
                            if (hasFirstPanto) pantograph1.AddMatrix(matrix); //some may have no first panto, will put it as panto 2
                            else pantograph2.AddMatrix(matrix);
                        }
                        break;
                }
            }
            else if(matrixName.StartsWith("MIRROR")) //Espejitos
            {
                mirrors.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("PANTO"))  // TODO, not sure why this is needed, see above!
            {
                Trace.TraceInformation("Pantograph matrix with unusual name {1} in shape {0}", TrainCarShape.SharedShape.FilePath, matrixName);
                if (matrixName.Contains("1"))
                    pantograph1.AddMatrix(matrix);
                else if (matrixName.Contains("2"))
                    pantograph2.AddMatrix(matrix);
                else if (matrixName.Contains("3"))
                    pantograph3.AddMatrix(matrix);
                else if (matrixName.Contains("4"))
                    pantograph4.AddMatrix(matrix);
                else
                {
                    if (hasFirstPanto) pantograph1.AddMatrix(matrix); //some may have no first panto, will put it as panto 2
                    else pantograph2.AddMatrix(matrix);
                }
            }
        }

        public override void PrepareFrame(RenderFrame frame, long elapsedTime)
        {
            pantograph1.updateState(false, elapsedTime);
            pantograph2.updateState(false, elapsedTime);
            leftDoor.updateState(false, elapsedTime);
            rightDoor.updateState(false,elapsedTime);
            mirrors.updateState(false, elapsedTime);
            UpdateAnimation(frame, elapsedTime,30); //TODO: Cambiar este valor por el de una velocidad real
        }

        private void UpdateAnimation(RenderFrame frame,long elapsedTime, float speed) //La velocidad es en metros/segundo
        {
            float wheelRadius = 0.98f; //Radio de todas las ruedas del vehículo
            float distanceTraveled = speed*((float)elapsedTime/1000)/(float)(2*Math.PI*wheelRadius); //Distancia recorrida por la rueda en esta unidad de tiempo
            if(WheelPartIndexes.Count>0)
            {
                double wheelCircumferenceM = MathHelper.TwoPi * wheelRadius;
                WheelRotationR = MathHelper.WrapAngle(WheelRotationR - distanceTraveled);
                Matrix wheelRotationMatrix = Matrix.CreateRotationX(WheelRotationR);
                foreach (int imatrix in WheelPartIndexes)
                {
                    TrainCarShape.XNAMatrices[imatrix] = wheelRotationMatrix * TrainCarShape.SharedShape.Matrices[imatrix];
                }
            }
            //Animación del ángulo del bogie
            foreach (TrainCarPart p in car.Parts)
            {
                if(p.iMatrix<=0) continue;
                Matrix m = Matrix.Identity;
                m.Translation = TrainCarShape.SharedShape.Matrices[p.iMatrix].Translation;
                m.M11 = p.Cos;
                m.M13 = p.Sin;
                m.M31 = -p.Sin;
                m.M33 = p.Cos;

            }
            TrainCarShape.PrepareFrame(frame, elapsedTime);
        }

        internal override void Mark()
        {
            TrainCarShape.Mark();
            if (null != InteriorShape)
                InteriorShape.Mark();
        }
    }

    public abstract class TrainCarViewer
    {
        public TrainCar car;
        protected Viewer viewer;

        public TrainCarViewer(Viewer viewer,TrainCar car)
        {
            this.car = car;
            this.viewer = viewer;
        }

        public abstract void PrepareFrame(RenderFrame frame, long elapsedTime);

        [CallOnThread("Loader")]
        public virtual void Unload() { }

        [CallOnThread("Loader")]
        internal virtual void LoadForPlayer() { }

        [CallOnThread("Loader")]
        internal abstract void Mark();       
    }
}
