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

namespace Solver_Bases
{
    public abstract class dealII_Base : Potential_Base
    {
        protected string dens_filename = "car_dens.dat";
        protected string densderiv_filename = "rho_prime.dat";
        protected string gphi_filename = "gphi.dat";

        protected Band_Data pot;

        protected int nz_donor;
        protected double zmin_donor, zmax_donor;

        protected string initcalc_parameterfile = "split_gate.in";
        protected string newton_parameterfile = "newton.in";

        string densdopent_filename = "dopent_1D.dat";

        public dealII_Base(bool external_code, Dictionary<string, object> input)
            : base(external_code)
        {
            if (!input.ContainsKey("initcalc_location") || !input.ContainsKey("newton_location"))
                throw new Exception("Error - To use deal.II you must provide the location for calculating the initial potential in \"initcalc_location\" and the newton step calculation in \"newton_location\"");

            this.initcalc_location = (string)input["initcalc_location"];
            this.newton_location = (string)input["newton_location"];

            if (input.ContainsKey("initcalc_parameterfile"))
                this.initcalc_parameterfile = (string)input["initcalc_parameterfile"];
            if (input.ContainsKey("newton_parameterfile"))
                this.newton_parameterfile = (string)input["newton_parameterfile"];

            // and save the dopent density 
            SpinResolved_Data dopents = (SpinResolved_Data)input["Dopent_Density"];
            Save_Data(dopents.Spin_Summed_Data, densdopent_filename);

            // and the necessary data to work out where these points are
            nz_donor = (int)(double)input["nz_pot_1d"];
            zmin_donor = (double)input["zmin_pot_1d"];
            zmax_donor = (double)input["zmax_pot_1d"];
        }

        protected override string[] Trim_Potential_File(string[] lines)
        {
            // deal.II code should return only potential on grid
            return lines;
        }

        protected override void Save_Data(Band_Data density, string input_file_name)
        {
            density.Save_Data(input_file_name);
        }

        protected override Band_Data Get_Pot_From_External(Band_Data density)
        {
            Save_Data(density, dens_filename);

            pot = Get_Data_From_External(initcalc_location, "-p " + initcalc_parameterfile, initcalc_result_filename);
            return pot;
        }

        public override Band_Data Calculate_Newton_Step(SpinResolved_Data rho_prime, Band_Data gphi)
        {
            Save_Data(rho_prime.Spin_Summed_Data, densderiv_filename);
            Save_Data(gphi, gphi_filename);

            Band_Data x = Get_Data_From_External(newton_location, "-p " + newton_parameterfile, newton_result_filename);
            return x;
        }

        public override Band_Data Calculate_Newton_Step(SpinResolved_Data rho_prime, Band_Data gphi, SpinResolved_Data car_dens, Band_Data dft_pot, Band_Data dft_calc)
        {
            Save_Data(car_dens.Spin_Summed_Data, dens_filename);
            Save_Data(dft_pot, xc_pot_filename);
            Save_Data(dft_calc, "xc_pot_calc.dat");

            return Calculate_Newton_Step(rho_prime, gphi);
        }

        public override Band_Data Chemical_Potential
        {
            get { return Physics_Base.q_e * pot; }
        }

        protected override Band_Data Get_Pot_On_Regular_Grid(Band_Data density)
        {
            throw new NotImplementedException();
        }
    }
}
