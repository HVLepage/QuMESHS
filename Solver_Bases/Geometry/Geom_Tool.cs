﻿/***************************************************************************
 * 
 * QuMESHS (Quantum Mesoscopic Electronic Semiconductor Heterostructure
 * Solver) for calculating electron and hole densities and electrostatic
 * potentials using self-consistent Poisson-Schroedinger solutions in 
 * layered semiconductors
 * 
 * Copyright(C) 2015 E. T. Owen and C. H. W. Barnes
 * 
 * The MIT License (MIT)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * For additional information, please contact eto24@cam.ac.uk or visit
 * <http://www.qumeshs.org>
 * 
 **************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Solver_Bases.Layers;

namespace Solver_Bases.Geometry
{
    public static class Geom_Tool
    {
        public static ILayer GetLayer(ILayer[] layers, double z)
        {
            return GetLayer(layers, 0.0, 0.0, z);
        }

        public static ILayer GetLayer(ILayer[] layers, double y, double z)
        {
            return GetLayer(layers, 0.0, y, z);
        }

        public static ILayer GetLayer(ILayer[] layers, double x, double y, double z)
        {
            ILayer result = null;
            for (int i = 0; i < layers.Length; i++)
                if (layers[i].InLayer(x, y, z))
                    result = layers[i];
            
            // if the layer is not found, throw an exception
            if (result == null)
                throw new Exception("Error - layer not found at (x, y, z) = (" + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + ")");

            if (result.Geometry == Geometry_Type.composite)
                return Extract_Layer_From_Composite(result, x, y, z);

            // else if the result is the substrate and this is not the surface, throw an exception
            if (result.Material == Material.Substrate && z != result.Zmax)
                throw new Exception("Error - layer at (x, y, z) = (" + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + ") is in the substrate");

            return result;
        }

        /// <summary>
        /// return the first layer you come across at this location (starting at component n and looking down)
        /// note that this does not check for overlap and will only return the default layer if there are no components at (x, y, z)
        /// </summary>
        private static ILayer Extract_Layer_From_Composite(ILayer composite, double x, double y, double z)
        {
            for (int i = composite.No_Components - 1; i >= 0; i--)
                if (composite.Get_Component(i).InLayer(x, y, z))
                    return composite.Get_Component(i);

            throw new FormatException("Error - We should have found the correct layer by this point.\nTheoretically impossible to get to this exception...");
        }

        public static ILayer GetLayer(ILayer[] layers, Plane plane, double x, double y, double z)
        {
            if (plane == Plane.xy)
                return GetLayer(layers, x, y, z);
            else if (plane == Plane.yz)
                return GetLayer(layers, z, x, y);
            else if (plane == Plane.zx)
                return GetLayer(layers, y, z, x);
            else
                throw new NotImplementedException();
        }

        public static ILayer Find_Layer_Below_Surface(ILayer[] layers)
        {
            for (int i = 0; i < layers.Length; i++)
                if (layers[i].Zmax == 0.0)
                    return layers[i];

            throw new Exception("Error - cannot find the layer immediately below the surface");
        }

        public static ILayer Find_Layer_Above_Surface(ILayer[] layers)
        {
            for (int i = 0; i < layers.Length; i++)
                if (layers[i].Zmin == 0.0)
                    return layers[i];

            throw new Exception("Error - cannot find the layer immediately below the surface");
        }

        public static double Get_Xmin(ILayer[] layers)
        {
            double result = double.MaxValue;

            // ignore the substrate
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].Material == Material.Substrate)
                    continue;

                if (layers[i].Xmin < result)
                    result = layers[i].Xmin;
            }

            return result;
        }

        public static double Get_Ymin(ILayer[] layers)
        {
            double result = double.MaxValue;

            // only look through the top n-1 layers as you know the bottom will be the substrate
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].Material == Material.Substrate)
                    continue;

                if (layers[i].Ymin < result)
                    result = layers[i].Ymin;
            }

            return result;
        }

        public static double Get_Zmin(ILayer[] layers)
        {
            double result = double.MaxValue;

            // only look through the top n-1 layers as you know the bottom will be the substrate
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].Material == Material.Substrate)
                    continue;

                if (layers[i].Zmin < result)
                    result = layers[i].Zmin;
            }

            return result;
        }
    }
}
