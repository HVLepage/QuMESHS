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
using Solver_Bases;
using Solver_Bases.Layers;
using CenterSpace.NMath.Core;

namespace ThreeD_SchrodingerPoissonSolver
{
    class ThreeD_ThomasFermiSolver : ThreeD_Density_Base
    {
        public ThreeD_ThomasFermiSolver(IExperiment exp)
            : base(exp)
        {
        }

        public override void Get_ChargeDensity(ILayer[] layers, ref SpinResolved_Data density, Band_Data chem_pot)
        {
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < ny; j++)
                    for (int k = 0; k < nz; k++)
                    {
                        double x = dx * i + xmin;
                        double y = dy * j + ymin;
                        double z = dz * k + zmin;

                        // get the relevant layer
                        ILayer current_Layer = Solver_Bases.Geometry.Geom_Tool.GetLayer(layers, x, y, z);

                        // calculate the density at the given point
                        ZeroD_Density charge_calc = new ZeroD_Density(current_Layer, temperature);
                        double local_charge_density = charge_calc.Get_CarrierDensity(chem_pot.vol[k][i, j]);

                        // as there is no spin dependence in this problem yet, just divide the charge into spin-up and spin-down components equally
                        density.Spin_Down.vol[k][i, j] = 0.5 * local_charge_density;
                        density.Spin_Up.vol[k][i, j] = 0.5 * local_charge_density;
                    }
        }

        public override SpinResolved_Data Get_ChargeDensity_Deriv(ILayer[] layers, SpinResolved_Data carrier_density_deriv, SpinResolved_Data dopent_density_deriv, Band_Data chem_pot)
        {
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < ny; j++)
                    for (int k = 0; k < nz; k++)
                    {
                        double x = dx * i + xmin;
                        double y = dy * j + ymin;
                        double z = dz * k + zmin;

                        // get the relevant layer and if it's frozen out, don't recalculate the dopent charge
                        ILayer current_Layer = Solver_Bases.Geometry.Geom_Tool.GetLayer(layers, x, y, z);

                        ZeroD_Density charge_calc = new ZeroD_Density(current_Layer, temperature);
                        if (!current_Layer.Dopents_Frozen_Out(temperature))
                        {
                            double local_dopent_density_deriv = charge_calc.Get_DopentDensityDeriv(chem_pot.vol[k][i, j]);
                            dopent_density_deriv.Spin_Up.vol[k][i, j] = 0.5 * local_dopent_density_deriv;
                            dopent_density_deriv.Spin_Down.vol[k][i, j] = 0.5 * local_dopent_density_deriv;
                        }
                        else
                        {
                            dopent_density_deriv.Spin_Up.vol[k][i, j] = 0.0;
                            dopent_density_deriv.Spin_Down.vol[k][i, j] = 0.0;
                        }

                        carrier_density_deriv.Spin_Up.vol[k][i, j] = 0.5 * charge_calc.Get_CarrierDensityDeriv(chem_pot.vol[k][i, j]);
                        carrier_density_deriv.Spin_Down.vol[k][i, j] = 0.5 * charge_calc.Get_CarrierDensityDeriv(chem_pot.vol[k][i, j]);
                    }

            return carrier_density_deriv + dopent_density_deriv;
        }

        public void Write_Out_Density(SpinResolved_Data h, string outfile)
        {
            Band_Data h_spin_summed = h.Spin_Summed_Data;

            System.IO.StreamWriter sw = new System.IO.StreamWriter(outfile);
            for (int k = 0; k < h_spin_summed.vol.Length; k++)
                for (int i = 0; i < h_spin_summed.vol[k].Cols; i++)
                    for (int j = 0; j < h_spin_summed.vol[k].Rows; j++)
                {
                    sw.Write(h_spin_summed.vol[k][i, j].ToString() + '\t');
                    if (i == h_spin_summed.vol[k].Cols - 1 || j == h_spin_summed.vol[k].Rows - 1)
                        sw.WriteLine();
                }

            sw.Close();
        }

        public override void Get_ChargeDensity_Deriv(ILayer[] layers, ref SpinResolved_Data density, Band_Data chem_pot)
        {
            throw new NotImplementedException();
        }

        public override DoubleVector Get_EnergyLevels(ILayer[] layers, Band_Data chem_pot)
        {
            throw new NotImplementedException();
        }
    }
}
