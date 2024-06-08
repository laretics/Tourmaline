#define SHOW_PHYSICS_GRAPHS     //Matej Pacha - if commented, the physics graphs are not ready for public release

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tourmaline.Simulation.RollingStocks;
using Tourmaline.Viewer3D.Processes;
using TOURMALINE.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Tourmaline.Viewer3D.Popups
{
    public class HUDWindow : LayeredWindow
    {
        // Set this to the width of each column in font-height units.
        readonly int ColumnWidth = 4;

        // Set to distance from top-left corner to place text.
        const int TextOffset = 10;

        readonly int ProcessorCount = System.Environment.ProcessorCount;

        readonly PerformanceCounter AllocatedBytesPerSecCounter; // \.NET CLR Memory(*)\Allocated Bytes/sec
        float AllocatedBytesPerSecLastValue;

        readonly Viewer Viewer;
        readonly Action<TableData>[] TextPages;
        readonly WindowTextFont TextFont;
        readonly HUDGraphMaterial HUDGraphMaterial;

        int TextPage;
        int LocomotivePage = 2;
        int LastTextPage;
        TableData TextTable = new TableData() { Cells = new string[0, 0] };

        HUDGraphSet ForceGraphs;
        HUDGraphMesh ForceGraphMotiveForce;
        HUDGraphMesh ForceGraphDynamicForce;
        HUDGraphMesh ForceGraphNumOfSubsteps;

        HUDGraphSet LocomotiveGraphs;
        HUDGraphMesh LocomotiveGraphsThrottle;
        HUDGraphMesh LocomotiveGraphsInputPower;
        HUDGraphMesh LocomotiveGraphsOutputPower;

        HUDGraphSet DebugGraphs;
        HUDGraphMesh DebugGraphMemory;
        HUDGraphMesh DebugGraphGCs;
        HUDGraphMesh DebugGraphFrameTime;
        HUDGraphMesh DebugGraphProcessRender;
        HUDGraphMesh DebugGraphProcessUpdater;
        HUDGraphMesh DebugGraphProcessLoader;
        HUDGraphMesh DebugGraphProcessSound;

        public HUDWindow(WindowManager owner)
            : base(owner, TextOffset, TextOffset, "HUD")
        {
            Viewer = owner.Viewer;
            LastTextPage = LocomotivePage;

            ProcessHandle = OpenProcess(0x410 /* PROCESS_QUERY_INFORMATION | PROCESS_VM_READ */, false, Process.GetCurrentProcess().Id);
            ProcessMemoryCounters = new PROCESS_MEMORY_COUNTERS() { Size = 40 };
            ProcessVirtualAddressLimit = GetVirtualAddressLimit();

            try
            {
                var counterDotNetClrMemory = new PerformanceCounterCategory(".NET CLR Memory");
                foreach (var process in counterDotNetClrMemory.GetInstanceNames())
                {
                    var processId = new PerformanceCounter(".NET CLR Memory", "Process ID", process);
                    if (processId.NextValue() == Process.GetCurrentProcess().Id)
                    {
                        AllocatedBytesPerSecCounter = new PerformanceCounter(".NET CLR Memory", "Allocated Bytes/sec", process);
                        break;
                    }
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
                Trace.TraceWarning("Unable to access Microsoft .NET Framework performance counters. This may be resolved by following the instructions at http://support.microsoft.com/kb/300956");
            }

            Debug.Assert(GC.MaxGeneration == 2, "Runtime is expected to have a MaxGeneration of 2.");

            var textPages = new List<Action<TableData>>();
            textPages.Add(TextPageDebugInfo);
            TextPages = textPages.ToArray();

            TextFont = owner.TextFontDefaultOutlined;
            ColumnWidth *= TextFont.Height;

            HUDGraphMaterial = (HUDGraphMaterial)Viewer.MaterialManager.Load("Debug");

            LocomotiveGraphs = new HUDGraphSet(Viewer, HUDGraphMaterial);
            LocomotiveGraphsOutputPower = LocomotiveGraphs.AddOverlapped(Color.Green, 50);

            ForceGraphs = new HUDGraphSet(Viewer, HUDGraphMaterial);
            ForceGraphDynamicForce = ForceGraphs.AddOverlapped(Color.Red, 75);

            DebugGraphs = new HUDGraphSet(Viewer, HUDGraphMaterial);
            DebugGraphMemory = DebugGraphs.Add("Memory", "0GB", String.Format("{0:F0}GB", (float)ProcessVirtualAddressLimit / 1024 / 1024 / 1024), Color.Orange, 50);
            DebugGraphGCs = DebugGraphs.Add("GCs", "0", "2", Color.Magenta, 20); // Multiple of 4
            DebugGraphFrameTime = DebugGraphs.Add("Frame time", "0.0s", "0.1s", Color.LightGreen, 50);
            DebugGraphProcessRender = DebugGraphs.Add("Render process", "0%", "100%", Color.Red, 20);
            DebugGraphProcessUpdater = DebugGraphs.Add("Updater process", "0%", "100%", Color.Yellow, 20);
            DebugGraphProcessLoader = DebugGraphs.Add("Loader process", "0%", "100%", Color.Magenta, 20);
            DebugGraphProcessSound = DebugGraphs.Add("Sound process", "0%", "100%", Color.Cyan, 20);
#if WITH_PATH_DEBUG
            TextPage = 5;
#endif
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(TextPage);
            outf.Write(LastTextPage);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            var page = inf.ReadInt32();
            if (page >= 0 && page <= TextPages.Length)
                TextPage = page;
            page = inf.ReadInt32();
            if (page > 0 && page <= TextPages.Length)
                LastTextPage = page;
            else LastTextPage = LocomotivePage;
        }

        public override void Mark()
        {
            base.Mark();
            HUDGraphMaterial.Mark();
        }

        public override bool Interactive
        {
            get
            {
                return false;
            }
        }

        public override void TabAction()
        {
            TextPage = (TextPage + 1) % TextPages.Length;
            if (TextPage != 0) LastTextPage = TextPage;
        }

        public void ToggleBasicHUD()
        {
            TextPage = TextPage == 0 ? LastTextPage : 0;
        }

        int[] lastGCCounts = new int[3];

        public override void PrepareFrame(RenderFrame frame, long elapsedTime, bool updateFull)
        {
            base.PrepareFrame(frame, elapsedTime, updateFull);
#if SHOW_PHYSICS_GRAPHS
#endif
            if (Visible && TextPages[TextPage] == TextPageDebugInfo)
            {
                var gcCounts = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
                DebugGraphMemory.AddSample((float)GetWorkingSetSize() / ProcessVirtualAddressLimit);
                DebugGraphGCs.AddSample(gcCounts[2] > lastGCCounts[2] ? 1.0f : gcCounts[1] > lastGCCounts[1] ? 0.5f : gcCounts[0] > lastGCCounts[0] ? 0.25f : 0);
                DebugGraphFrameTime.AddSample(Viewer.RenderProcess.FrameTime.Value * 10);                
                DebugGraphProcessUpdater.AddSample(Viewer.UpdaterProcess.Profiler.Wall.Value / 100);
                DebugGraphProcessLoader.AddSample(Viewer.LoaderProcess.Profiler.Wall.Value / 100);
                lastGCCounts = gcCounts;
                DebugGraphs.PrepareFrame(frame);
            }
        }

        public override void PrepareFrame(long elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                var table = new TableData() { Cells = new string[TextTable.Cells.GetLength(0), TextTable.Cells.GetLength(1)] };
                TextPages[0](table);
                if (TextPage > 0)
                    TextPages[TextPage](table);
                TextTable = table;
            }
        }

        // ==========================================================================================================================================
        //      Method to construct the various Heads Up Display pages for use by the WebServer 
        //      Replaces the Prepare Frame Method
        //      djr - 20171221
        // ==========================================================================================================================================
        public TableData PrepareTable(int PageNo)
        {
            var table = new TableData() { Cells = new string[1, 1] };

            TextPages[PageNo](table);
            return (table);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Completely customise the rendering of the HUD - don't call base.Draw(spriteBatch).
            for (var row = 0; row < TextTable.Cells.GetLength(0); row++)
            {
                for (var column = 0; column < TextTable.Cells.GetLength(1); column++)
                {
                    if (TextTable.Cells[row, column] != null)
                    {
                        var text = TextTable.Cells[row, column];
                        var align = text.StartsWith(" ") ? LabelAlignment.Right : LabelAlignment.Left;
                        var color = Color.White;
                        if (text.EndsWith("!!!") || text.EndsWith("???"))
                        {
                            color = text.EndsWith("!!!") ? Color.OrangeRed : Color.Yellow;
                            text = text.Substring(0, text.Length - 3);
                        }
                        else if (text.EndsWith("%%%"))
                        {
                            color = Color.Cyan;
                            text = text.Substring(0, text.Length - 3);
                        }
                        else if (text.EndsWith("$$$"))
                        {
                            color = Color.Pink;
                            text = text.Substring(0, text.Length - 3);
                        }
                        TextFont.Draw(spriteBatch, new Rectangle(TextOffset + column * ColumnWidth, TextOffset + row * TextFont.Height, ColumnWidth, TextFont.Height), Point.Zero, text, align, color);
                    }
                }
            }

            if (Visible && TextPages[TextPage] == TextPageDebugInfo)
                DebugGraphs.Draw(spriteBatch);
        }

        #region Table handling


        // ==========================================================================================================================================
        //      Class used to construct table for display of Heads Up Display pages
        //      Original Code has been altered making the class public for use by the WebServer
        //      djr - 20171221
        // ==========================================================================================================================================
        //sealed class TableData
        //{
        //    public string[,] Cells;
        //    public int CurrentRow;
        //    public int CurrentLabelColumn;
        //    public int CurrentValueColumn;
        //}

        public sealed class TableData
        {
            public string[,] Cells;
            public int CurrentRow;
            public int CurrentLabelColumn;
            public int CurrentValueColumn;
        }

        static void TableSetCell(TableData table, int cellColumn, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, cellColumn, format, args);
        }

        static void TableSetCell(TableData table, int cellRow, int cellColumn, string format, params object[] args)
        {
            if (cellRow > table.Cells.GetUpperBound(0) || cellColumn > table.Cells.GetUpperBound(1))
            {
                var newCells = new string[Math.Max(cellRow + 1, table.Cells.GetLength(0)), Math.Max(cellColumn + 1, table.Cells.GetLength(1))];
                for (var row = 0; row < table.Cells.GetLength(0); row++)
                    for (var column = 0; column < table.Cells.GetLength(1); column++)
                        newCells[row, column] = table.Cells[row, column];
                table.Cells = newCells;
            }
            Debug.Assert(!format.Contains('\n'), "HUD table cells must not contain newlines. Use the table positioning instead.");
            table.Cells[cellRow, cellColumn] = args.Length > 0 ? String.Format(format, args) : format;
        }

        static void TableSetCells(TableData table, int startColumn, params string[] columns)
        {
            for (var i = 0; i < columns.Length; i++)
                TableSetCell(table, startColumn + i, columns[i]);
        }

        static void TableAddLine(TableData table)
        {
            table.CurrentRow++;
        }

        static void TableAddLine(TableData table, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, 0, format, args);
            table.CurrentRow++;
        }

        static void TableAddLines(TableData table, string lines)
        {
            if (lines == null)
                return;

            foreach (var line in lines.Split('\n'))
            {
                var column = 0;
                foreach (var cell in line.Split('\t'))
                    TableSetCell(table, column++, "{0}", cell);
                table.CurrentRow++;
            }
        }

        static void TableSetLabelValueColumns(TableData table, int labelColumn, int valueColumn)
        {
            table.CurrentLabelColumn = labelColumn;
            table.CurrentValueColumn = valueColumn;
        }

        static void TableAddLabelValue(TableData table, string label, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, table.CurrentLabelColumn, label);
            TableSetCell(table, table.CurrentRow, table.CurrentValueColumn, format, args);
            table.CurrentRow++;
        }
        #endregion
       
#if WITH_PATH_DEBUG
        void TextPagePathInfo(AITrain thisTrain, TableData table)
        {
            // next is active AI trains
            if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
            {
                var status = thisTrain.GetPathStatus(Viewer.MilepostUnitsMetric);
                status = thisTrain.AddPathInfo(status, Viewer.MilepostUnitsMetric);
                for (var iCell = 0; iCell < status.Length; iCell++)
                    TableSetCell(table, table.CurrentRow, iCell, status[iCell]);
                TableAddLine(table);
            }
        }

        void TextPageActionsInfo(AITrain thisTrain, TableData table)
        {
            // next is active AI trains
            if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
            {
                var status = thisTrain.GetActionStatus(Viewer.MilepostUnitsMetric);
                for (var iCell = 0; iCell < status.Length; iCell++)
                    TableSetCell(table, table.CurrentRow, iCell, status[iCell]);
                TableAddLine(table);
            }
        }
#endif

        void TextPageDebugInfo(TableData table)
        {
            TableSetLabelValueColumns(table, 0, 2);
            TextPageHeading(table, "INFORMACION DE DEPURACIÓN");

            var allocatedBytesPerSecond = AllocatedBytesPerSecCounter == null ? 0 : AllocatedBytesPerSecCounter.NextValue();
            if (allocatedBytesPerSecond >= 1 && AllocatedBytesPerSecLastValue != allocatedBytesPerSecond)
                AllocatedBytesPerSecLastValue = allocatedBytesPerSecond;

            //TableAddLabelValue(table, "Logging enabled", Viewer.Settings.DataLogger ? "Yes" : "No");
            TableAddLabelValue(table, "Build", VersionInfo.Build);
            //TableAddLabelValue(table, "GPU", string.Format("{0:F0} FPS (50th/95th/99th percentiles {1:F1} / {2:F1} / {3:F1} ms, DirectX feature level >= {4})", Viewer.RenderProcess.FrameRate.SmoothedValue, Viewer.RenderProcess.FrameTime.SmoothedP50 * 1000, Viewer.RenderProcess.FrameTime.SmoothedP95 * 1000, Viewer.RenderProcess.FrameTime.SmoothedP99 * 1000, Viewer.Settings.DirectXFeatureLevel));
            TableAddLabelValue(table, "GPU", string.Format("{0:F0} FPS (50th/95th/99th percentiles {1:F1} / {2:F1} / {3:F1} ms, DirectX feature level >= {4})", Viewer.RenderProcess.FrameRate.SmoothedValue, Viewer.RenderProcess.FrameTime.SmoothedP50 * 1000, Viewer.RenderProcess.FrameTime.SmoothedP95 * 1000, Viewer.RenderProcess.FrameTime.SmoothedP99 * 1000, "popo"));
            TableAddLabelValue(table, "Adapter", string.Format("{0} ({1:F0} MB)", Viewer.AdapterDescription, Viewer.AdapterMemory / 1024 / 1024));
            //if (Viewer.Settings.DynamicShadows)
            //{
            //    TableSetCells(table, 3, Enumerable.Range(0, RenderProcess.ShadowMapCount).Select(i => String.Format(string.Format("{0}/{1}", RenderProcess.ShadowMapDistance[i], RenderProcess.ShadowMapDiameter[i]))).ToArray());
            //    TableSetCell(table, 3 + RenderProcess.ShadowMapCount, string.Format("({0}x{0})", Viewer.Settings.ShadowMapResolution));
            //    TableAddLine(table, "Shadow maps");
            //    TableSetCells(table, 3, Viewer.RenderProcess.ShadowPrimitivePerFrame.Select(p => p.ToString("F0")).ToArray());
            //    TableAddLabelValue(table, "Shadow primitives", string.Format("{0:F0}", Viewer.RenderProcess.ShadowPrimitivePerFrame.Sum()));
            //}
            TableSetCells(table, 3, Viewer.RenderProcess.PrimitivePerFrame.Select(p => p.ToString("F0")).ToArray());
            TableAddLabelValue(table, "Render primitives", string.Format("{0:F0}", Viewer.RenderProcess.PrimitivePerFrame.Sum()));
            TableAddLabelValue(table, "Updater process", string.Format("{0:F0}% ({1:F0}% {2})", Viewer.UpdaterProcess.Profiler.Wall.SmoothedValue, Viewer.UpdaterProcess.Profiler.Wait.SmoothedValue, "wait"));
            TableAddLabelValue(table, "Loader process", string.Format("{0:F0}% ({1:F0}% {2})", Viewer.LoaderProcess.Profiler.Wall.SmoothedValue, Viewer.LoaderProcess.Profiler.Wait.SmoothedValue, "wait"));
            TableAddLine(table);
        }

        static void TextPageHeading(TableData table, string name)
        {
            TableAddLine(table);
            TableAddLine(table, name);
        }

        #region Native code
        [StructLayout(LayoutKind.Sequential, Size = 64)]
        public class MEMORYSTATUSEX
        {
            public uint Size;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }

        [StructLayout(LayoutKind.Sequential, Size = 40)]
        struct PROCESS_MEMORY_COUNTERS
        {
            public int Size;
            public int PageFaultCount;
            public int PeakWorkingSetSize;
            public int WorkingSetSize;
            public int QuotaPeakPagedPoolUsage;
            public int QuotaPagedPoolUsage;
            public int QuotaPeakNonPagedPoolUsage;
            public int QuotaNonPagedPoolUsage;
            public int PagefileUsage;
            public int PeakPagefileUsage;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX buffer);

        [DllImport("psapi.dll", SetLastError = true)]
        static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, int size);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        readonly IntPtr ProcessHandle;
        PROCESS_MEMORY_COUNTERS ProcessMemoryCounters;
        readonly ulong ProcessVirtualAddressLimit;
        #endregion

        public uint GetWorkingSetSize()
        {
            // Get memory usage (working set).
            GetProcessMemoryInfo(ProcessHandle, out ProcessMemoryCounters, ProcessMemoryCounters.Size);
            return (uint)ProcessMemoryCounters.WorkingSetSize;
        }

        public ulong GetVirtualAddressLimit()
        {
            var buffer = new MEMORYSTATUSEX { Size = 64 };
            GlobalMemoryStatusEx(buffer);
            return Math.Min(buffer.TotalVirtual, buffer.TotalPhysical);
        }
    }

    public class HUDGraphSet
    {
        readonly Viewer Viewer;
        readonly Material Material;
        readonly Vector2 Margin = new Vector2(40, 10);
        readonly int Spacing;
        readonly List<Graph> Graphs = new List<Graph>();

        public HUDGraphSet(Viewer viewer, Material material)
        {
            Viewer = viewer;
            Material = material;
            Spacing = Viewer.WindowManager.TextFontSmallOutlined.Height + 2;
        }

        public HUDGraphMesh AddOverlapped(Color color, int height)
        {
            return Add("", "", "", color, height, true);
        }

        public HUDGraphMesh Add(string labelName, string labelMin, string labelMax, Color color, int height)
        {
            return Add(labelName, labelMin, labelMax, color, height, false);
        }

        HUDGraphMesh Add(string labelName, string labelMin, string labelMax, Color color, int height, bool overlapped)
        {
            HUDGraphMesh mesh;
            Graphs.Add(new Graph()
            {
                Mesh = mesh = new HUDGraphMesh(Viewer, color, height),
                LabelName = labelName,
                LabelMin = labelMin,
                LabelMax = labelMax,
                Overlapped = overlapped,
            });
            for (var i = Graphs.Count - 1; i >= 0; i--)
            {
                var previousGraphs = Graphs.Skip(i + 1).Where(g => !g.Overlapped);
                Graphs[i].YOffset = (int)previousGraphs.Sum(g => g.Mesh.GraphPos.W) + Spacing * previousGraphs.Count();
            }
            return mesh;
        }

        public void PrepareFrame(RenderFrame frame)
        {
            var matrix = Matrix.Identity;
            for (var i = 0; i < Graphs.Count; i++)
            {
                Graphs[i].Mesh.GraphPos.X = Viewer.DisplaySize.X - Margin.X - Graphs[i].Mesh.GraphPos.Z;
                Graphs[i].Mesh.GraphPos.Y = Margin.Y + Graphs[i].YOffset;
                frame.AddPrimitive(Material, Graphs[i].Mesh, RenderPrimitiveGroup.Overlay, ref matrix);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var box = new Rectangle();
            for (var i = 0; i < Graphs.Count; i++)
            {
                if (!string.IsNullOrEmpty(Graphs[i].LabelName))
                {
                    box.X = (int)Graphs[i].Mesh.GraphPos.X;
                    box.Y = Viewer.DisplaySize.Y - (int)Graphs[i].Mesh.GraphPos.Y - (int)Graphs[i].Mesh.GraphPos.W - Spacing;
                    box.Width = (int)Graphs[i].Mesh.GraphPos.Z;
                    box.Height = Spacing;
                    Viewer.WindowManager.TextFontSmallOutlined.Draw(spriteBatch, box, Point.Zero, Graphs[i].LabelName, LabelAlignment.Right, Color.White);
                    box.X = box.Right + 3;
                    box.Y += Spacing - 3;
                    Viewer.WindowManager.TextFontSmallOutlined.Draw(spriteBatch, box.Location, Graphs[i].LabelMax, Color.White);
                    box.Y += (int)Graphs[i].Mesh.GraphPos.W - Spacing + 7;
                    Viewer.WindowManager.TextFontSmallOutlined.Draw(spriteBatch, box.Location, Graphs[i].LabelMin, Color.White);
                }
            }
        }

        class Graph
        {
            public HUDGraphMesh Mesh;
            public string LabelName;
            public string LabelMin;
            public string LabelMax;
            public int YOffset;
            public bool Overlapped;
        }
    }

    public class HUDGraphMesh : RenderPrimitive
    {
        const int SampleCount = 1024 - 10 - 40; // Widest graphs we can fit in 1024x768.
        const int VerticiesPerSample = 6;
        const int PrimitivesPerSample = 2;
        const int VertexCount = VerticiesPerSample * SampleCount;

        readonly DynamicVertexBuffer VertexBuffer;
        readonly VertexBuffer BorderVertexBuffer;
        readonly Color Color;

        int SampleIndex;
        VertexPositionColor[] Samples = new VertexPositionColor[VertexCount];

        public Vector4 GraphPos; // xy = xy position, zw = width/height
        public Vector2 Sample; // x = index, y = count

        public HUDGraphMesh(Viewer viewer, Color color, int height)
        {
            VertexBuffer = new DynamicVertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionColor), VertexCount, BufferUsage.WriteOnly);
            BorderVertexBuffer = new VertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionColor), 10, BufferUsage.WriteOnly);
            var borderOffset = new Vector2(1f / SampleCount, 1f / height);
            var borderColor = new Color(Color.White, 0);
            BorderVertexBuffer.SetData(new[] {
                // Bottom left
                new VertexPositionColor(new Vector3(0 - borderOffset.X, 0 - borderOffset.Y, 1), borderColor),
                new VertexPositionColor(new Vector3(0, 0, 1), borderColor),
                // Bottom right
                new VertexPositionColor(new Vector3(1 + borderOffset.X, 0 - borderOffset.Y, 0), borderColor),
                new VertexPositionColor(new Vector3(1, 0, 0), borderColor),
                // Top right
                new VertexPositionColor(new Vector3(1 + borderOffset.X, 1 + borderOffset.Y, 0), borderColor),
                new VertexPositionColor(new Vector3(1, 1, 0), borderColor),
                // Top left
                new VertexPositionColor(new Vector3(0 - borderOffset.X, 1 + borderOffset.Y, 1), borderColor),
                new VertexPositionColor(new Vector3(0, 1, 1), borderColor),
                // Bottom left
                new VertexPositionColor(new Vector3(0 - borderOffset.X, 0 - borderOffset.Y, 1), borderColor),
                new VertexPositionColor(new Vector3(0, 0, 1), borderColor),
            });
            Color = color;
            Color.A = 255;
            GraphPos.Z = SampleCount;
            GraphPos.W = height;
            Sample.Y = SampleCount;
        }

        void VertexBuffer_ContentLost()
        {
            VertexBuffer.SetData(0, Samples, 0, Samples.Length, VertexPositionColor.VertexDeclaration.VertexStride, SetDataOptions.NoOverwrite);
        }

        public void AddSample(float value)
        {
            value = MathHelper.Clamp(value, 0, 1);
            var x = Sample.X / Sample.Y;

            Samples[(int)Sample.X * VerticiesPerSample + 0] = new VertexPositionColor(new Vector3(x, value, 0), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 1] = new VertexPositionColor(new Vector3(x, value, 1), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 2] = new VertexPositionColor(new Vector3(x, 0, 1), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 3] = new VertexPositionColor(new Vector3(x, 0, 1), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 4] = new VertexPositionColor(new Vector3(x, value, 0), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 5] = new VertexPositionColor(new Vector3(x, 0, 0), Color);
            VertexBuffer.SetData((int)Sample.X * VerticiesPerSample * VertexPositionColor.VertexDeclaration.VertexStride, Samples, (int)Sample.X * VerticiesPerSample, VerticiesPerSample, VertexPositionColor.VertexDeclaration.VertexStride, SetDataOptions.NoOverwrite);

            SampleIndex = (SampleIndex + 1) % SampleCount;
            Sample.X = SampleIndex;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (VertexBuffer.IsContentLost)
                VertexBuffer_ContentLost();

            // Draw border
            graphicsDevice.SetVertexBuffer(BorderVertexBuffer);
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 8);

            // Draw graph area (skipping the next value to be written)
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            if (SampleIndex > 0)
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, SampleIndex * PrimitivesPerSample);
            if (SampleIndex + 1 < SampleCount)
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, (SampleIndex + 1) * VerticiesPerSample, (SampleCount - SampleIndex - 1) * PrimitivesPerSample);
        }
    }

    public class HUDGraphMaterial : Material
    {
        IEnumerator<EffectPass> ShaderPassesGraph;

        public HUDGraphMaterial(Viewer viewer)
            : base(viewer, null)
        {
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.DebugShader;
            shader.CurrentTechnique = shader.Techniques["Graph"];
            if (ShaderPassesGraph == null) ShaderPassesGraph = shader.Techniques["Graph"].Passes.GetEnumerator();
            shader.ScreenSize = new Vector2(Viewer.DisplaySize.X, Viewer.DisplaySize.Y);

            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            graphicsDevice.DepthStencilState = DepthStencilState.None;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.DebugShader;

            ShaderPassesGraph.Reset();
            while (ShaderPassesGraph.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    var graphMesh = item.RenderPrimitive as HUDGraphMesh;
                    if (graphMesh != null)
                    {
                        shader.GraphPos = graphMesh.GraphPos;
                        shader.GraphSample = graphMesh.Sample;
                        ShaderPassesGraph.Current.Apply();
                    }
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }
    }
}
