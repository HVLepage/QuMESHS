﻿/***************************************************************************
 * 
 * QuMESHS (Quantum Mesoscopic Electronic Semiconductor Heterostructure
 * Solver) for calculating electron and hole densities and electrostatic
 * potentials using self-consistent Poisson-Schroedinger solutions in 
 * layered semiconductors
 * 
 * Copyright(C) 2015 E. T. Owen and C. H. W. Barnes
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * For additional information, please contact eto24@cam.ac.uk or visit
 * <http://www.qumeshs.org>
 * 
 **************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Solver_Bases.Geometry;

namespace Solver_Bases.Layers
{
    public class AlGaAs_Layer : Layer
    {
        double x;
        double permitivity_bowing_ratio = 0.0;
        
        public AlGaAs_Layer(IGeom geom, int layer_no, double alloy_ratio)
            : base(geom, layer_no)
        {
            this.x = alloy_ratio;
            // re-set material parameters with correct alloy ratio
            Set_Material_Parameters();
        }

        protected override void Set_Material_Parameters()
        {
            material = Material.AlGaAs;
            permitivity = (x * Physics_Base.epsilon_r_AlAs + (1 - x) * Physics_Base.epsilon_r_GaAs + permitivity_bowing_ratio * x * (1 - x)) * Physics_Base.epsilon_0;

            // set the AlGaAs band gap and acceptor/donor energies are positive and show how far from the band gap centre the donors are
            if (x < 0.45)
                this.band_gap = 1424.0 + 1247.0 * x;
            else
                this.band_gap = 1900.0 + 125.0 * x + 143.0 * x * x;

            allow_donors = true;
            this.acceptor_energy = -0.5 * band_gap + 35.0; this.donor_energy = 0.5 * band_gap - 5.0;

            // set the electron and hole masses
            hole_mass = (0.51 + 0.25 * x) * Physics_Base.m_e;
            /*
            if (x < 0.45)
                electron_mass = (0.063 + 0.083 * x) * Physics_Base.m_e;
            else
                electron_mass = (0.85 - 0.14 * x) * Physics_Base.m_e;
            */
        }

        internal override void Set_Freeze_Out_Temperature()
        {
            // and set a default freeze-out temperature for the dopents of 70K
            freeze_out_temp = 70.0;
        }
    }
}
