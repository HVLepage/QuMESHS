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
    public abstract class Layer : ILayer
    {
        // layer information
        IGeom geom;
        protected Material material;
        protected double permitivity;
        int layer_no;

        // material parameters
        protected double band_gap;
        protected bool allow_donors = false;        // by default, don't allow donors to be set
        protected double donor_energy;
        protected double acceptor_energy;
        protected double donor_concentration = 0.0;
        protected double acceptor_concentration = 0.0;
        protected double freeze_out_temp = 0.0;     // default for Layer is no freezing out of donors

        // default electron and hole mass is for GaAs
        protected double electron_mass = 0.067 * Physics_Base.m_e;
        protected double hole_mass = 0.51 * Physics_Base.m_e;
                
        public Layer(IGeom Geom, int layer_no)
        {
            this.layer_no=layer_no;
            this.geom = Geom;
            Set_Material_Parameters();
        }

        protected abstract void Set_Material_Parameters();
        public void Set_Dopents(double acceptor_concentration, double donor_concentration)
        {
            if (!allow_donors)
                throw new NotImplementedException("Error - you are not allowed to add donors to layer no " + Layer_No.ToString());
            this.acceptor_concentration = acceptor_concentration; this.donor_concentration = donor_concentration;
            Set_Freeze_Out_Temperature();
        }
        public double Permitivity { get { return permitivity; } }
        public Material Material { get { return material; } }
        internal abstract void Set_Freeze_Out_Temperature();
        internal void Set_Freeze_Out_Temperature(double freeze_out_T)
        {
            if (!allow_donors)
                throw new NotImplementedException("Error - you are not allowed to add donors to layer no " + Layer_No.ToString());
            this.freeze_out_temp = freeze_out_T;
        }

        /// <summary>
        /// returns true if the input temperature is less than the temperature at which the donors are frozen out
        /// </summary>
        public bool Dopents_Frozen_Out(double temperature)
        {
            return temperature < freeze_out_temp;
        }

        #region Access_Geometry
        public bool InLayer(double z)
        { return geom.InLayer(z); }
        public bool InLayer(double y, double z)
        { return geom.InLayer(y, z); }
        public bool InLayer(double x, double y, double z)
        { return geom.InLayer(x, y, z); }
        public Geometry_Type Geometry { get { return geom.Get_Geometry; } }
        public ILayer Get_Component(int component_no) { return this; }
        public int No_Components { get { return 1; } }

        public double Xmin { get { return geom.Xmin; } }
        public double Xmax { get { return geom.Xmax; } }
        public double Ymin { get { return geom.Ymin; } }
        public double Ymax { get { return geom.Ymax; } }
        public double Zmin { get { return geom.Zmin; } }
        public double Zmax { get { return geom.Zmax; } }
        #endregion

        #region Layer_Properties
        public int Layer_No { get { return layer_no; } }
        public double Band_Gap { get { return band_gap; } }
        public double Donor_Energy { get { return donor_energy; } }
        public double Acceptor_Energy { get { return acceptor_energy; } }
        public double Donor_Conc { get { return donor_concentration; } }
        public double Acceptor_Conc { get { return acceptor_concentration; } }
        public double Dopent_FreezeOut_T { get { return freeze_out_temp; } }
        #endregion

        public double Electron_Mass { get { return electron_mass; } }
        public double Hole_Mass { get { return hole_mass; } }
    }
}