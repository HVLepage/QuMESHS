﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CenterSpace.NMath.Core;
using Solver_Bases.Layers;
using Solver_Bases.Geometry;

namespace Solver_Bases
{
    public abstract class Experiment_Base
    {
        protected SpinResolved_Data charge_density;
        protected Band_Data chem_pot;
        protected ILayer[] layers;

        protected double bottom_bc;

        // parameters for the density domain
        protected double dx_dens, dy_dens, dz_dens;
        protected double xmin_dens, ymin_dens, zmin_dens = -1.0;
        protected int nx_dens, ny_dens, nz_dens;

        // parameters for the potential domain
        protected double dx_pot, dy_pot, dz_pot;
        protected double xmin_pot, ymin_pot, zmin_pot = -1.0;
        protected int nx_pot, ny_pot, nz_pot;

        protected double alpha, alpha_prime, tol;

        protected bool using_flexPDE = false;
        protected string flexPDE_input;
        protected string flexPDE_location;

        protected double initial_temperature = 300.0;
        protected double temperature;

        protected bool TF_only = false;       // only run Thomas-Fermi density calculations (i.e. no DFT, Green's functions etc...)

        protected void Initialise(Dictionary<string, object> input_dict)
        {
            // solver inputs
            Get_From_Dictionary<double>(input_dict, "tolerance", ref tol);
            Get_From_Dictionary<double>(input_dict, "alpha", ref alpha);
            Get_From_Dictionary<double>(input_dict, "alpha", ref alpha_prime);

            // will not use FlexPDE unless told to
            if (input_dict.ContainsKey("use_FlexPDE")) this.using_flexPDE = bool.Parse((string)input_dict["use_FlexPDE"]); else using_flexPDE = false;
            // default input file for FlexPDE is called "default.pde"
            if (input_dict.ContainsKey("FlexPDE_file")) this.flexPDE_input = (string)input_dict["FlexPDE_file"]; else this.flexPDE_input = "default.pde";
            if (using_flexPDE)
            {
                // make sure that FlexPDE does exist at the specified location
                try { this.flexPDE_location = (string)input_dict["FlexPDE_location"]; }
                catch (Exception) { throw new Exception("Error - no location for FlexPDE executable supplied"); }
                if (!File.Exists(flexPDE_location))
                    throw new Exception("Error - FlexPDE executable file does not exist at location" + flexPDE_location + "!");
            }

            // physical inputs
            Get_From_Dictionary<double>(input_dict, "init_T", ref initial_temperature, true);
            Get_From_Dictionary<double>(input_dict, "T", ref temperature);

            // get the band structure
            if (input_dict.ContainsKey("Layers"))
                layers = (ILayer[])input_dict["Layers"];
            else
            {
                if (input_dict.ContainsKey("BandStructure_File"))
                {
                    layers = Input_Band_Structure.Get_Layers((string)input_dict["BandStructure_File"]);
                    input_dict.Add("Layers", layers);
                }
                else throw new KeyNotFoundException("No band structure file found in input dictionary!");
            }

            // and find the domain minimum coordinate values
            xmin_pot = Geom_Tool.Get_Xmin(layers);
            ymin_pot = Geom_Tool.Get_Ymin(layers);
            zmin_pot = Geom_Tool.Get_Zmin(layers);
            // but still try to get them from the dictionary if its there
            Get_From_Dictionary<double>(input_dict, "xmin_pot", ref xmin_pot, true);
            Get_From_Dictionary<double>(input_dict, "ymin_pot", ref ymin_pot, true);
            Get_From_Dictionary<double>(input_dict, "zmin_pot", ref zmin_pot, true);
            
            // and by default these are the same as for the density
            xmin_dens = xmin_pot;
            ymin_dens = ymin_pot;
            zmin_dens = zmin_pot;
            // and, once again, try to find something in the dictionary
            Get_From_Dictionary<double>(input_dict, "xmin_dens", ref xmin_dens, true);
            Get_From_Dictionary<double>(input_dict, "ymin_dens", ref ymin_dens, true);
            Get_From_Dictionary<double>(input_dict, "zmin_dens", ref zmin_dens, true);
        }

        public abstract void Run();

        /// <summary>
        /// returns a list of dopent freeze-out temperatures between the initial and final temperatures of the experiment
        /// and with the final temperature at the end
        /// </summary>
        protected double[] Freeze_Out_Temperatures()
        {
            double[] raw_temps = new double[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                raw_temps[i] = layers[i].Dopent_FreezeOut_T;

            // now, calculate which temperatures are in the range (final_T < T < init_T) and sort in descending order
            double[] temp_list = (from value in raw_temps where (value > temperature && value < initial_temperature) select value).ToArray().Concat(new[] {temperature}).ToArray();
            return temp_list.Distinct().OrderByDescending(c => c).ToArray();
        }

        /// <summary>
        /// gets a value of the type "T" from the input dictionary
        /// DOES NOT WORK FOR INTs
        /// </summary>
        /// <param name="attempt_only"> set to true if you don't want this method to throw an Exception</param>
        protected void Get_From_Dictionary<T>(Dictionary<string, object> input, string key, ref T value, bool attempt_only = false)
        {
            if (input.ContainsKey(key))
                value = (T)input[key];
            else if (!attempt_only)
                throw new KeyNotFoundException("Error - cannot find the key: " + key);
        }

        protected void Get_From_Dictionary(Dictionary<string, object> input, string key, ref int value, bool attempt_only = false)
        {
            if (input.ContainsKey(key))
                value = (int)(double)input[key];
            else if (!attempt_only)
                throw new KeyNotFoundException("Error - cannot find the key: " + key);
        }

        protected void Get_From_Dictionary<T>(Dictionary<string, object> input, string key, ref T value, T default_value)
        {
            Get_From_Dictionary<T>(input, key, ref default_value, true);
        }

        public SpinResolved_Data Charge_Density
        {
            get { return charge_density; }
        }
        public Band_Data Chemical_Potential
        {
            get { return chem_pot; }
        }

        public ILayer[] Layers
        {
            get { return layers; }
        }

        public double Temperature
        {
            get { return temperature; }
        }

        public double Bottom_BC
        {
            get { return bottom_bc; }
        }

        public int Nx_Dens
        {
            get { return nx_dens; }
        }

        public double Dx_Dens
        {
            get { return dx_dens; }
        }

        public double Xmin_Dens
        {
            get { return xmin_dens; }
        }
        public int Ny_Dens
        {
            get { return ny_dens; }
        }

        public double Dy_Dens
        {
            get { return dy_dens; }
        }

        public double Ymin_Dens
        {
            get { return ymin_dens; }
        }
        public int Nz_Dens
        {
            get { return nz_dens; }
        }

        public double Dz_Dens
        {
            get { return dz_dens; }
        }

        public double Zmin_Dens
        {
            get { return zmin_dens; }
        }

        public int Nx_Pot
        {
            get { return nx_pot; }
        }

        public double Dx_Pot
        {
            get { return dx_pot; }
        }

        public double Xmin_Pot
        {
            get { return xmin_pot; }
        }
        public int Ny_Pot
        {
            get { return ny_pot; }
        }

        public double Dy_Pot
        {
            get { return dy_pot; }
        }

        public double Ymin_Pot
        {
            get { return ymin_pot; }
        }
        public int Nz_Pot
        {
            get { return nz_pot; }
        }

        public double Dz_Pot
        {
            get { return dz_pot; }
        }

        public double Zmin_Pot
        {
            get { return zmin_pot; }
        }
    }
}
