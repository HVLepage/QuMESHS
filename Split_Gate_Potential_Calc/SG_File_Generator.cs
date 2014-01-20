﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Solver_Bases;
using Solver_Bases.Layers;
using Solver_Bases.Geometry;

namespace Split_Gate_Potential_Calc
{
    class SG_File_Generator
    {
        Dictionary<string, object> input;
        ILayer[] layers;
        int ny, nz;
        double dy, dz;
        double ymin, zmin;
        double bottom_bc;
        string rbf_fit;

        public SG_File_Generator(Dictionary<string, object> input)
        {
            this.input = input;
            this.layers = (ILayer[])input["Layers"];

            this.dy = (double)input["dy"]; this.ny = (int)(double)input["ny"];
            this.dz = (double)input["dz"]; this.nz = (int)(double)input["nz"];
            this.ymin = (double)input["ymin"]; this.zmin = (double)input["zmin"];

            // Calculate the bottom boundary condition
            OneD_ThomasFermiPoisson.OneD_ThomasFermiSolver dens_solv = new OneD_ThomasFermiPoisson.OneD_ThomasFermiSolver((double)input["T"], (double)input["dz"], (int)(double)input["nz"], (double)input["zmin"]);
            bottom_bc = dens_solv.Get_Chemical_Potential(Geom_Tool.Get_Zmin(layers), layers);

            // get the donor densities and positions
            Fit_Donor_Level_RBF(input);
        }

        void Fit_Donor_Level_RBF(Dictionary<string, object> input)
        {
            // find the donor layer
            int[] donor_layer = Layer_Tool.Get_Donor_Layers(layers);
            if (donor_layer.Length != 1) throw new NotImplementedException("Error - at the moment there can only be one donor layer in the sample!");

            // and get some of its parameters
            double donor_layer_width = layers[donor_layer[0]].Zmax - layers[donor_layer[0]].Zmin;
            int no_pos = (int)Math.Round(donor_layer_width / (double)input["dz"]);
            int donor_base_index = (int)(Math.Round(layers[donor_layer[0]].Zmin - Geom_Tool.Get_Zmin(layers)) / (double)input["dz"]);

            // calculate the positions and densities to make the RBF fit to
            double[] positions = new double[no_pos];
            double[] donor_dens = new double[no_pos];
            for (int i = 0; i < no_pos; i++)
            {
                positions[i] = layers[donor_layer[0]].Zmin + i * (double)input["dz"];
                donor_dens[i] = ((CenterSpace.NMath.Core.DoubleVector)input["oned_dens_data"])[donor_base_index + i];
            }

            // and do the fit here
            OneD_ThomasFermiPoisson.OneD_RBF_Fit rbf_fitter = new OneD_ThomasFermiPoisson.OneD_RBF_Fit(positions, 3.0 * (double)input["dz"]);
            rbf_fitter.Get_RBF_Weights(donor_dens);
            rbf_fit = rbf_fitter.Get_RBF_Equation("donor_dens", "y");
        }

        internal void Generate_2D_FlexPDE_File(TwoD_ThomasFermiPoisson.Experiment exp, double surface)
        {
            string output_file = "split_gate_2d.pde";
            
            Check_Output_Filename(ref output_file);

            TwoD_ThomasFermiPoisson.TwoD_PoissonSolver temp_poissolv = new TwoD_ThomasFermiPoisson.TwoD_PoissonSolver(exp, true, output_file, null, 0.0);
            temp_poissolv.Create_FlexPDE_File(surface, bottom_bc, output_file);
        }

        public void Generate_3D_FlexPDE_File(double surface)
        {
            // calculate size of x dimension
            double lx = (double)input["dx"] * (double)input["nx"];

            // set up output stream
            string output_filename = "split_gate_3d.pde";
            Check_Output_Filename(ref output_filename);
            StreamWriter sw = new StreamWriter(output_filename);

            // write out output file
            sw.WriteLine("TITLE \'Full Split Gate Geometry\'");
            sw.WriteLine("COORDINATES cartesian3");
            sw.WriteLine();
            sw.WriteLine("VARIABLES");
	        sw.WriteLine("\tu");
            sw.WriteLine();
            sw.WriteLine("SELECT");
	        sw.WriteLine("\tERRLIM=1e-4");
            sw.WriteLine("DEFINITIONS");
	        sw.WriteLine("\trho = 0.0");
	        sw.WriteLine("\tband_gap = " + layers[layers.Length - 1].Band_Gap.ToString());
            sw.WriteLine(); 
	        sw.WriteLine("\tlx = " + lx.ToString());
	        sw.WriteLine("\tly = " + (dy * ny).ToString());
	        sw.WriteLine("\tlz = " + (dz * nz).ToString());
            sw.WriteLine();
	        sw.WriteLine("\tbottom_bc = " + bottom_bc.ToString());
	        sw.WriteLine("\tsurface_bc = " + surface.ToString());
            sw.WriteLine();
	        sw.WriteLine("\t! GATE VOLTAGE INPUTS (in V)");
	        sw.WriteLine("\tsplit_V = 0.0");
	        sw.WriteLine("\ttop_V = 0.0");
            sw.WriteLine();
	        sw.WriteLine("\t! SPLIT GATE DIMENSIONS (in nm)");
	        sw.WriteLine("\tsplit_width = 600" + "");
	        sw.WriteLine("\tsplit_length = 400" + "");
	        sw.WriteLine("\tsplit_depth = 10 " + "" + " ! depth of the split gate metal material");
            sw.WriteLine();
	        sw.WriteLine("\t! WELL DEPTH (in nm)");
	        sw.WriteLine("\twell_depth = 91" + "");
            sw.WriteLine();
            sw.WriteLine("\t! Electrical permitivities");
            sw.WriteLine("\teps_0 = " + Physics_Base.epsilon_0.ToString());
            // relative permitivity of materials
            sw.WriteLine("\teps_r_GaAs = " + Physics_Base.epsilon_r_GaAs.ToString());
            sw.WriteLine("\teps_r_AlGaAs = " + Physics_Base.epsilon_r_AlGaAs.ToString());
            sw.WriteLine("\teps_pmma = " + Physics_Base.epsilon_pmma.ToString());
            sw.WriteLine("\teps");
            sw.WriteLine();
	        sw.WriteLine("\tq_e = " + Physics_Base.q_e.ToString() + "! charge of electron in zC");
            sw.WriteLine();
            sw.WriteLine("EQUATIONS");
	        sw.WriteLine("\tu: div(eps * grad(u)) = - rho	! Poisson's equation");
            sw.WriteLine();
            sw.WriteLine("EXTRUSION");
            sw.WriteLine("\tSURFACE \"Substrate\"\tz = " + Geom_Tool.Get_Zmin(layers).ToString());
            for (int i = 1; i < layers.Length; i++)
            {
                sw.WriteLine("\t\tLayer \"" + i.ToString() + "\"");
                sw.WriteLine("\tSURFACE	\"" + i.ToString() + "\"\tz = " + layers[i].Zmax.ToString());
            }
            sw.WriteLine();
            sw.WriteLine("BOUNDARIES");
	        sw.WriteLine("\tSURFACE \"Substrate\"	VALUE(u) = bottom_bc");
            sw.WriteLine("\tSURFACE \"" + Geom_Tool.Find_Layer_Below_Surface(layers).ToString() + "\" NATURAL(u) = surface_bc");
            sw.WriteLine("\tSURFACE \"" + layers.Length.ToString() + "\" VALUE(u) = top_V");
            sw.WriteLine();
            sw.WriteLine("\tREGION 1");
            for (int i = 1; i < layers.Length; i++)
            {
                sw.WriteLine("\t\tLAYER \"" + i.ToString() + "\"");
                if (layers[i].Acceptor_Conc != 0.0 || layers[i].Donor_Conc != 0.0)
                    sw.WriteLine("\t\trho = TABLE(\'dens_3D.dat\', x, y)");
                    //sw.WriteLine("\t\trho = donor_dens");
                else
                    sw.WriteLine("\t\trho = 0.0");
                sw.WriteLine("\t\teps = " + Layer_Tool.Get_Permitivity(layers[i].Material));
                sw.WriteLine("\t\tband_gap = " + layers[i].Band_Gap.ToString());
                sw.WriteLine();
            }
            sw.WriteLine();
	        sw.WriteLine("\t\tSTART(-lx / 2, -ly / 2)");
		    sw.WriteLine("\t\tLINE TO (-lx / 2, ly / 2)");
		    sw.WriteLine("\t\tLINE TO (lx / 2, ly / 2)");
		    sw.WriteLine("\t\tLINE TO (lx / 2, -ly / 2)");
		    sw.WriteLine("\t\tLINE TO CLOSE");
            sw.WriteLine();
        	sw.WriteLine("\tLIMITED REGION 2");
            sw.WriteLine("\t\tSURFACE \"" + Geom_Tool.Find_Layer_Below_Surface(layers).ToString() + "\" VALUE(u) = split_V");
            sw.WriteLine("\t\tSURFACE \"" + Geom_Tool.Find_Layer_Above_Surface(layers).ToString() + "\" VALUE(u) = split_V");
            sw.WriteLine("\t\tLAYER \"" + Geom_Tool.Find_Layer_Above_Surface(layers).ToString() + "\" VOID");
        	sw.WriteLine("\t\tSTART (-lx / 2, -split_length / 2)");
        	sw.WriteLine("\t\tLINE TO (-lx / 2, split_length / 2)");
        	sw.WriteLine("\t\tVALUE(u) = split_V");
        	sw.WriteLine("\t\tLINE TO (-split_width / 2, split_length / 2) TO (-split_width / 2, -split_length / 2) TO CLOSE");
        	sw.WriteLine();
        	sw.WriteLine("\tLIMITED REGION 3");
            sw.WriteLine("\t\tSURFACE \"" + Geom_Tool.Find_Layer_Below_Surface(layers).ToString() + "\" VALUE(u) = split_V");
            sw.WriteLine("\t\tSURFACE \"" + Geom_Tool.Find_Layer_Above_Surface(layers).ToString() + "\" VALUE(u) = split_V");
            sw.WriteLine("\t\tLAYER \"" + Geom_Tool.Find_Layer_Above_Surface(layers).ToString() + "\" VOID");
        	sw.WriteLine("\t\tSTART (lx / 2, -split_length / 2)");
        	sw.WriteLine("\t\tLINE TO (lx / 2, split_length / 2)");
        	sw.WriteLine("\t\tVALUE(u) = split_V");
        	sw.WriteLine("\t\tLINE TO (split_width / 2, split_length / 2) TO (split_width / 2, -split_length / 2) TO CLOSE");
            sw.WriteLine();
            sw.WriteLine("MONITORS");
            sw.WriteLine("\tCONTOUR(rho) ON x = 0");
            sw.WriteLine("\tCONTOUR(u) ON x = 0");
            sw.WriteLine("\tCONTOUR(u) ON y = 0");
            sw.WriteLine("\tCONTOUR(u) ON z = -well_depth");
            sw.WriteLine("PLOTS");
            sw.WriteLine("\tCONTOUR(rho) ON x = 0");
            sw.WriteLine("\tCONTOUR(u) ON x = 0");
            sw.WriteLine("\tCONTOUR(u) ON y = 0");
            sw.WriteLine("\tCONTOUR(u) ON z = -well_depth" + "");
            sw.WriteLine("\tCONTOUR(-q_e * u + 0.5 * band_gap) ON z = -well_depth");
            sw.WriteLine("\tELEVATION(rho) FROM (0,0, " + Geom_Tool.Get_Zmin(layers).ToString() + ") TO (0, 0, " + layers[layers.Length - 1].Zmax.ToString() + ")");
            sw.WriteLine("\tELEVATION(-q_e * u + 0.5 * band_gap) FROM (0, 0, " + Geom_Tool.Get_Zmin(layers).ToString() + ") TO (0, 0, " + layers[layers.Length - 1].Zmax.ToString() + ")");
            sw.WriteLine("\tELEVATION(-q_e * u + 0.5 * band_gap) FROM (0, -ly / 2, -well_depth) TO (0, ly / 2, -well_depth)");
            sw.WriteLine("\tELEVATION(-q_e * u + 0.5 * band_gap) FROM (-lx/2, 0, -well_depth) TO (lx / 2, 0, -well_depth)");
            sw.WriteLine();
            sw.WriteLine("TRANSFER (rho, u) FILE=\"data_file.dat\"");
            sw.WriteLine();
            sw.WriteLine("END");

            sw.Close();
        }

        /// <summary>
        /// checks to see whether the file already exists and asks the user if it should be replaced if it does
        /// </summary>
        void Check_Output_Filename(ref string output_file)
        {
            if (File.Exists(output_file))
            {
                bool problem_resolved = false;
                while (!problem_resolved)
                {
                    Console.WriteLine(output_file + " already exists, do you want to replace it y/n");
                    char input_key = Console.ReadKey().KeyChar;
                    if (input_key == 'Y' || input_key == 'y')
                    {
                        File.Delete(output_file);
                        problem_resolved = true;
                    }
                    else if (input_key == 'N' || input_key == 'n')
                    {
                        Console.WriteLine("Input file name to save FlexPDE file to");
                        output_file = Console.ReadLine();
                        problem_resolved = true;
                    }
                    else
                        Console.WriteLine(input_key + " - Invalid input");
                }
            }
        }
    }
}
