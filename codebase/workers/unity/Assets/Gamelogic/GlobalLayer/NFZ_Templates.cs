using System;
using Improbable.Controller;

namespace AssemblyCSharp.Gamelogic.GlobalLayer
{
    public static class NFZ_Templates
    {
        private static double[] basic = { 
            0, 1, 
            2, 2
        };

        public static Improbable.Controller.NoFlyZone GetNoFlyZone(NFZTemplate template)
        {
            Improbable.Controller.NoFlyZone nfz = new Improbable.Controller.NoFlyZone();
            nfz.vertices = new Improbable.Collections.List<Improbable.Vector3d>();

            double[] coords = basic;
            switch (template)
            {
                case NFZTemplate.BASIC:
                default:
                    break;
            }

            for (int i = 0; i < coords.Length; i += 2)
            {
                nfz.vertices.Add(new Improbable.Vector3d(coords[i], 0, coords[i+1]));
            }

            NoFlyZone.setBoundingBoxCoordinates(ref nfz);

            return nfz;
        }
    }
}
