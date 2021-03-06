﻿/***************************************************************************
 * 
 * QuMESHS (Quantum Mesoscopic Electronic Semiconductor Heterostructure
 * Solver) for calculating electron and hole densities and electrostatic
 * potentials using self-consistent Poisson-Schroedinger solutions in 
 * layered semiconductors
 * 
 * Copyright(C) 2015 E. T. Owen, T. Gemunden and C. H. W. Barnes
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
using System.Threading.Tasks;
using Solver_Bases;

namespace Iterative_Greens_Function_Test
{
    public class Experiment : Experiment_Base
    {
        public Experiment()
            : base()
        {
        }

        public override void Initialise(Dictionary<string, object> input_dict)
        {
            //base.Initialise(input_dict);
        }

        public override bool Run()
        {
            Iterative_Greens_Function iter = new Iterative_Greens_Function(this);
            iter.Iterate();

            //throw new NotImplementedException();
            return true;
        }
  

  /* public double[,] CalcDoS(double energy, double[,] potential)
        {
            Iterative_Greens_Function iter = new Iterative_Greens_Function(this, potential);

            double[,] DoS = iter.GetDoS(energy);

            return DoS;
        }*/

        protected override void Initialise_DataClasses(Dictionary<string, object> input_dict)
        {
            throw new NotImplementedException();
        }

        protected override void Initialise_from_Hot_Start(Dictionary<string, object> input_dict)
        {
            throw new NotImplementedException();
        }

        protected override bool Run_Iteration_Routine(IDensity_Solve dens_solv, IPoisson_Solve pois_solv, double tol, int max_iterations)
        {
            throw new NotImplementedException();
        }
    }
}
