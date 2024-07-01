using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tourmaline.Viewer3D.TvForms
{
    internal class _3dCamera
    {
        internal _3dTrainViewer viewer { private set; get; }

        internal _3dCamera(_3dTrainViewer viewer)
        {
            this.viewer = viewer;
        }
    }
}
