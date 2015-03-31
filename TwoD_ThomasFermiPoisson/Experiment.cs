﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CenterSpace.NMath.Core;
using Solver_Bases;
using Solver_Bases.Layers;
using Solver_Bases.Geometry;
using Solver_Bases.Mixing_Schedulers;

namespace TwoD_ThomasFermiPoisson
{
    public class Experiment : Experiment_Base
    {
        Dictionary<string, double> device_dimensions, boundary_conditions;
        double t_damp = 0.8, t_min = 1e-3;

        TwoD_ThomasFermiSolver dens_solv;
        IPoisson_Solve pois_solv;

        public void Initialise_Experiment(Dictionary<string, object> input_dict)
        {
            Console.WriteLine("Initialising Experiment");

            // simulation domain inputs
            Get_From_Dictionary<double>(input_dict, "dy", ref dy_dens); dy_pot = dy_dens;
            Get_From_Dictionary<double>(input_dict, "dz", ref dz_dens); dz_pot = dz_dens;

            Get_From_Dictionary(input_dict, "ny", ref ny_dens); ny_pot = ny_dens;
            Get_From_Dictionary(input_dict, "nz", ref nz_dens); nz_pot = nz_dens;

            // but try to get the specific values
            Get_From_Dictionary<double>(input_dict, "dy_dens", ref dy_dens, true);
            Get_From_Dictionary<double>(input_dict, "dz_dens", ref dz_dens, true);
            Get_From_Dictionary<double>(input_dict, "dy_pot", ref dy_pot, true);
            Get_From_Dictionary<double>(input_dict, "dz_pot", ref dz_pot, true);

            Get_From_Dictionary(input_dict, "ny_dens", ref ny_dens, true);
            Get_From_Dictionary(input_dict, "nz_dens", ref nz_dens, true);
            Get_From_Dictionary(input_dict, "ny_pot", ref ny_pot, true);
            Get_From_Dictionary(input_dict, "nz_pot", ref nz_pot, true);

            // physics parameters are done by the base method
            base.Initialise(input_dict);

            // and split gate dimensions
            device_dimensions = new Dictionary<string, double>();
            device_dimensions.Add("split_width", (double)input_dict["split_width"]);
            device_dimensions.Add("zmin_pot", this.Layers[1].Zmin);
            device_dimensions.Add("pmma_depth", Geom_Tool.Find_Layer_Above_Surface(this.Layers).Zmax);
            device_dimensions.Add("cap_depth", Geom_Tool.Find_Layer_Below_Surface(this.Layers).Zmin);
            device_dimensions.Add("interface_depth", this.Layers[1].Zmax);
            device_dimensions.Add("buffer_depth", this.Layers[2].Zmax);

            // gate voltages
            boundary_conditions = new Dictionary<string, double>();
            boundary_conditions.Add("top_V", (double)input_dict["top_V"]);
            boundary_conditions.Add("split_V", (double)input_dict["split_V"]);
            boundary_conditions.Add("bottom_V", 0.0);

            // try to get the density from the dictionary... they probably won't be there and if not... make them
            if (input_dict.ContainsKey("hot_start")) hot_start = (bool)input_dict["hot_start"];
            if (hot_start)
            {
                // load (spin-resolved) density data
                string[] spin_up_data, spin_down_data;
                try
                {
                    spin_up_data = File.ReadAllLines((string)input_dict["spin_up_file"]);
                    spin_down_data = File.ReadAllLines((string)input_dict["spin_down_file"]);
                }
                catch (KeyNotFoundException key_e)
                { throw new Exception("Error - Are the file names for the hot start data included in the input file?\n" + key_e.Message); }

                this.carrier_density = new SpinResolved_Data(Band_Data.Parse_Band_Data(spin_up_data, Ny_Dens, Nz_Dens), Band_Data.Parse_Band_Data(spin_down_data, Ny_Dens, Nz_Dens));

                // and surface charge density
                try { boundary_conditions.Add("surface", double.Parse(File.ReadAllLines((string)input_dict["surface_charge_file"])[0])); }
                catch (KeyNotFoundException key_e) { throw new Exception("Error - Are the file names for the hot start data included in the input file?\n" + key_e.Message); }
            }
            else if (input_dict.ContainsKey("SpinResolved_Density"))
            {
                this.carrier_density = new SpinResolved_Data(new Band_Data(new DoubleMatrix(ny_dens, nz_dens)), new Band_Data(new DoubleMatrix(ny_dens, nz_dens)));

                SpinResolved_Data tmp_1d_density = (SpinResolved_Data)input_dict["SpinResolved_Density"];

                int offset_min = (int)Math.Round((zmin_dens - zmin_pot) / dz_pot);

                for (int i = 0; i < ny_dens; i++)
                    for (int j = 0; j < nz_dens; j++)
                    {
                        // do not add anything to the density if on the edge of the domain
                        if (i == 0 || i == ny_dens - 1 || j == 0 || j == nz_dens - 1)
                            continue;

                        // linearly interpolate with exponential envelope in the transverse direction
                        int j_init = (int)Math.Floor(j * Dz_Dens / Dz_Pot);
                        double y = (i - 0.5 * ny_dens) * dy_dens;
                        //double z = (j - 0.5 * nz_dens) * dz_dens;
                        double envelope = 0.0;//Math.Exp(-100.0 * y * y / ((double)(ny_dens * ny_dens) * dy_dens * dy_dens));// *Math.Exp(-10.0 * z * z / ((double)(nz_dens * nz_dens) * dz_dens * dz_dens)); 
                        carrier_density.Spin_Up.mat[i, j] = envelope * tmp_1d_density.Spin_Up.vec[offset_min + j_init];// +(tmp_1d_density.Spin_Up.vec[offset_min + j_init + 1] - tmp_1d_density.Spin_Up.vec[offset_min + j_init]) * Dz_Dens / Dz_Pot;
                        carrier_density.Spin_Down.mat[i, j] = envelope * tmp_1d_density.Spin_Down.vec[offset_min + j_init];// +(tmp_1d_density.Spin_Down.vec[offset_min + j_init + 1] - tmp_1d_density.Spin_Down.vec[offset_min + j_init]) * Dz_Dens / Dz_Pot;
                    }

                boundary_conditions.Add("surface", (double)input_dict["surface_charge"]);
            }
            else
            {
                this.carrier_density = new SpinResolved_Data(new Band_Data(new DoubleMatrix(ny_dens, nz_dens)), new Band_Data(new DoubleMatrix(ny_dens, nz_dens)));
                boundary_conditions.Add("surface", (double)input_dict["surface_charge"]);
            }

            // and try to get the dopent density data
            if (input_dict.ContainsKey("Dopent_Density"))
            {
                this.dopent_density = new SpinResolved_Data(new Band_Data(new DoubleMatrix(ny_dens, nz_dens)), new Band_Data(new DoubleMatrix(ny_dens, nz_dens)));
                SpinResolved_Data tmp_1d_dopdens = (SpinResolved_Data)input_dict["Dopent_Density"];

                int offset_min = (int)Math.Round((zmin_dens - zmin_pot) / dz_pot);
                for (int i = 0; i < ny_dens; i++)
                    for (int j = 0; j < nz_dens; j++)
                    {
                        // do not add anything to the density if on the edge of the domain
                        if (i == 0 || i == ny_dens - 1 || j == 0 || j == nz_dens - 1)
                            continue;

                        // linearly interpolate with exponential envelope in the transverse direction
                        int j_init = (int)Math.Floor(j * Dz_Dens / Dz_Pot);
                        dopent_density.Spin_Up.mat[i, j] = tmp_1d_dopdens.Spin_Up.vec[offset_min + j_init] + (tmp_1d_dopdens.Spin_Up.vec[offset_min + j_init + 1] - tmp_1d_dopdens.Spin_Up.vec[offset_min + j_init]) * Dz_Dens / Dz_Pot;
                        dopent_density.Spin_Down.mat[i, j] = tmp_1d_dopdens.Spin_Down.vec[offset_min + j_init] + (tmp_1d_dopdens.Spin_Down.vec[offset_min + j_init + 1] - tmp_1d_dopdens.Spin_Down.vec[offset_min + j_init]) * Dz_Dens / Dz_Pot;
                    }
            }
            else
                this.dopent_density = new SpinResolved_Data(new Band_Data(new DoubleMatrix(ny_dens, nz_dens)), new Band_Data(new DoubleMatrix(ny_dens, nz_dens)));

            // and instantiate their derivatives
            carrier_density_deriv = new SpinResolved_Data(new Band_Data(new DoubleMatrix(ny_dens, nz_dens)), new Band_Data(new DoubleMatrix(ny_dens, nz_dens)));
            dopent_density_deriv = new SpinResolved_Data(new Band_Data(new DoubleMatrix(ny_dens, nz_dens)), new Band_Data(new DoubleMatrix(ny_dens, nz_dens)));

            // and do the same for the chemical potential
            if (input_dict.ContainsKey("Chemical_Potential"))
            {
                chem_pot = new Band_Data(ny_dens, nz_dens, 0.0);
                Band_Data tmp_pot_1d = (Band_Data)input_dict["Chemical_Potential"];

                int offset_min = (int)Math.Round((zmin_dens - zmin_pot) / dz_pot);
                for (int i = 0; i < ny_dens; i++)
                    for (int j = 0; j < nz_dens; j++)
                    {
                        chem_pot.mat[i, j] = tmp_pot_1d.vec[offset_min + j - 1];
                    }
            }
            else
                chem_pot = new Band_Data(ny_dens, nz_dens, 0.0);

            // create charge density solver and calculate boundary conditions
            dens_solv = new TwoD_ThomasFermiSolver(this);

            // initialise potential solver
            if (using_flexPDE)
                pois_solv = new TwoD_PoissonSolver_Scaled(this, using_flexPDE, input_dict);
            else if (using_dealii)
                pois_solv = new TwoD_dealII_Solver(this, using_dealii, input_dict);
            else
                throw new NotImplementedException("Error - Must use either FlexPDE or deal.II for 2D potential solver!");

            Console.WriteLine("Experimental parameters initialised");
        }

        public override void Run()
        {
            // calculate the bare potential
            Console.WriteLine("Calculating bare potential");
            pois_solv.Initiate_Poisson_Solver(device_dimensions, boundary_conditions);
            chem_pot = pois_solv.Get_Chemical_Potential(0.0 * carrier_density.Spin_Summed_Data);
            Console.WriteLine("Saving bare potential");
            (Input_Band_Structure.Get_BandStructure_Grid(layers, dy_dens, dz_dens, ny_dens, nz_dens, ymin_dens, zmin_dens) - chem_pot).Save_Data("bare_pot.dat");
            Console.WriteLine("Bare potential saved");

            // and then run the DFT solver at the base temperature over a limited range
    //        TwoD_DFTSolver dft_solv = new TwoD_DFTSolver(this);
            TwoD_EffectiveBandSolver dft_solv = new TwoD_EffectiveBandSolver(this);
            dft_solv.Xmin_Pot = ymin_pot; dft_solv.Dx_Pot = dy_pot;
            dft_solv.Ymin_Pot = zmin_pot; dft_solv.Dy_Pot = dz_pot;
      //      TwoD_ThomasFermiSolver dft_solv = new TwoD_ThomasFermiSolver(this);
            
       //     // run without DFT
       //     dft_solv.Set_DFT_Mixing_Parameter(0.0);
       //     Run_Iteration_Routine(dft_solv, 0.1, 1000);

            bool converged = false;
            int no_runs = 200;
            if (no_dft)
                dft_solv.DFT_Mixing_Parameter = 0.0;
            else
                dft_solv.DFT_Mixing_Parameter = 0.1;
            // start without dft if carrier density is empty
            if (carrier_density.Spin_Summed_Data.Min() == 0.0)
                dft_solv.DFT_Mixing_Parameter = 0.0;

            // run the iteration routine!
            converged = Run_Iteration_Routine(dft_solv, pois_solv, tol, no_runs);

            // initialise output solvers
            TwoD_ThomasFermiSolver final_dens_solv = new TwoD_ThomasFermiSolver(this);

            // save final density out
            carrier_density.Spin_Summed_Data.Save_Data("dens_2D_raw.dat");
            carrier_density.Spin_Up.Save_Data("dens_2D_up_raw.dat");
            carrier_density.Spin_Down.Save_Data("dens_2D_down_raw.dat");

            // save surface charge
            StreamWriter sw = new StreamWriter("surface_charge.dat"); sw.WriteLine(boundary_conditions["surface"].ToString()); sw.Close();
            // save eigen-energies
            DoubleVector energies = dft_solv.Get_EnergyLevels(layers, chem_pot);
            StreamWriter sw_e = new StreamWriter("energies.dat");
            for (int i = 0; i < energies.Length; i++)
                sw_e.WriteLine(energies[i]);
            sw_e.Close();

   //         dft_solv.Get_ChargeDensity(layers, carrier_density, dopent_density, chem_pot).Spin_Summed_Data.Save_Data("dens_2D_raw_calc.dat");
            final_dens_solv.Output(carrier_density, "carrier_density.dat");
            final_dens_solv.Output(carrier_density - dft_solv.Get_ChargeDensity(layers, carrier_density, dopent_density, chem_pot), "density_error.dat");
            (Input_Band_Structure.Get_BandStructure_Grid(layers, dy_dens, dz_dens, ny_dens, nz_dens, ymin_dens, zmin_dens) - chem_pot).Save_Data("potential.dat");
            Band_Data pot_exc = dft_solv.DFT_diff(carrier_density) + Physics_Base.Get_XC_Potential(carrier_density);
            pot_exc.Save_Data("xc_pot.dat");
            (Input_Band_Structure.Get_BandStructure_Grid(layers, dy_dens, dz_dens, ny_dens, nz_dens, ymin_dens, zmin_dens) - chem_pot + pot_exc).Save_Data("pot_KS.dat");
   //         Band_Data ks_ke = dft_solv.Get_KS_KE(layers, chem_pot);
   //         ks_ke.Save_Data("ks_ke.dat");
        }


        bool Run_Iteration_Routine(IDensity_Solve dens_solv, IPoisson_Solve pois_solv, double pot_lim)
        {
            return Run_Iteration_Routine(dens_solv, pois_solv, pot_lim, int.MaxValue);
        }

        double dens_diff_lim = 0.1; // the maximum percentage change in the density required for update of V_xc
        double pot_diff_lim = 0.1; // minimum value change (in meV) at which the iteration will stop
        double max_vxc_diff = double.MaxValue; // maximum difference for dft potential... if this increases, the dft mixing parameter is reduced
        double min_dens_diff = 0.005; // minimum bound for the required, percentage density difference for updating the dft potential
        double min_vxc_diff = 0.1; // minimum difference in the dft potential for convergence
        double min_alpha = 0.03; // minimum possible value of the dft mixing parameter
        bool Run_Iteration_Routine(IDensity_Solve dens_solv, IPoisson_Solve pois_solv, double pot_lim, int max_count)
        {
            // calculate initial potential with the given charge distribution
        //    Console.WriteLine("Calculating initial potential grid");
       //     pois_solv.Initiate_Poisson_Solver(device_dimensions, boundary_conditions);
        //    chem_pot = pois_solv.Get_Chemical_Potential(carrier_density.Spin_Summed_Data);
        //    Console.WriteLine("Initial grid complete");
            dens_solv.Set_DFT_Potential(carrier_density);
            dens_solv.Get_ChargeDensity(layers, ref carrier_density, ref dopent_density, chem_pot);
            dens_solv.Set_DFT_Potential(carrier_density); 
            
            int count = 0;
            bool converged = false;
            if (!no_dft)
                dens_solv.DFT_Mixing_Parameter = 0.1;
            dens_diff_lim = 0.12;
            while (!converged)
            {
                Stopwatch stpwch = new Stopwatch();
                stpwch.Start();

                // save old density data
                Band_Data dens_old = carrier_density.Spin_Summed_Data.DeepenThisCopy();

                // Get charge rho(phi) (not dopents as these are included as a flexPDE input)
                dens_solv.Get_ChargeDensity(layers, ref carrier_density, ref dopent_density, chem_pot);

                // Generate an approximate charge-dependent part of the Jacobian, g'(phi) = - d(eps * d( )) - rho'(phi) using the Thomas-Fermi semi-classical method
                SpinResolved_Data rho_prime = dens_solv.Get_ChargeDensity_Deriv(layers, carrier_density_deriv, dopent_density_deriv, chem_pot);

                // Solve stepping equation to find raw Newton iteration step, g'(phi) x = - g(phi)
                Band_Data gphi = -1.0 * pois_solv.Calculate_Laplacian(chem_pot / Physics_Base.q_e) - carrier_density.Spin_Summed_Data;
                Band_Data x = pois_solv.Calculate_Newton_Step(rho_prime, gphi, carrier_density, dens_solv.DFT_diff(carrier_density));
                chem_pot = pois_solv.Chemical_Potential;
                
                // Calculate optimal damping parameter, t, (but damped damping....)
                if (t == 0.0)
                    t = t_min;

                double t_old = t;
                t = t_damp * Calculate_optimal_t(t / t_damp, chem_pot, x, carrier_density, dopent_density, pois_solv, dens_solv, t_min);
                if (t_old == t && t_old == t_damp * t_min)
                {
                    t_min *= 2.0;
                    Console.WriteLine("Iterator has stalled, doubling t_min to " + t_min.ToString());
                }

                // and check convergence of density
                Band_Data dens_diff = carrier_density.Spin_Summed_Data - dens_old;
                Band_Data car_dens_spin_summed = carrier_density.Spin_Summed_Data;
                double carrier_dens_min = Math.Abs(car_dens_spin_summed.Min());
                // using the relative absolute density difference
                for (int i = 0; i < dens_diff.Length; i++)
                    // only calculate density difference for densities more than 1% of the maximum value
                    if (Math.Abs(car_dens_spin_summed[i]) > 0.01 * carrier_dens_min)
                        dens_diff[i] = Math.Abs(dens_diff[i] / car_dens_spin_summed[i]);
                    else
                        dens_diff[i] = 0.0;
                
                //if (Math.Max(t * x.Max(), (-t * x).Max()) < pot_lim && t > 10.0 * t_min)

                // only renew DFT potential when the difference in density has converged and the iterator has done at least 3 iterations
                if (dens_diff.Max() < dens_diff_lim && t > 10.0 * t_min && count > 3)              
                {
                    // once dft potential is starting to be mixed in, set the maximum count to lots
//                    max_count = 1000;

                    // and set the DFT potential
                    if (dens_solv.DFT_Mixing_Parameter != 0.0)
                        dens_solv.Print_DFT_diff(carrier_density);
                    dens_solv.Set_DFT_Potential(carrier_density);

                    // also... if the difference in the old and new dft potentials is greater than for the previous V_xc update, reduce the dft mixing parameter
                    double current_vxc_diff = Math.Max(dens_solv.DFT_diff(carrier_density).Max(), (-1.0 * dens_solv.DFT_diff(carrier_density).Min()));
              //      if (current_dens_diff > max_diff && dens_solv.DFT_Mixing_Parameter / 3.0 > min_alpha)
              //      {
              //          dens_solv.DFT_Mixing_Parameter /= 3.0;      // alpha is only incremented if it will be above the value of min_alpha
              //          dens_diff_lim /= 3.0;
              //          Console.WriteLine("DFT mixing parameter reduced to " + dens_solv.DFT_Mixing_Parameter.ToString());
              //      }
                    if ((current_vxc_diff > max_vxc_diff && dens_diff_lim / 2.0 > min_dens_diff) || no_dft)
                    {
                        dens_diff_lim /= 2.0;
                        Console.WriteLine("Minimum percentage density difference reduced to " + dens_diff_lim.ToString());
                    }
                    max_vxc_diff = current_vxc_diff;

                   // if (alpha_dft <= 0.1)
                   // {
                   //     alpha_dft += 0.01;
                   //     Console.WriteLine("Setting DFT mixing parameter to " + alpha_dft.ToString());
                   //     dens_solv.Set_DFT_Mixing_Parameter(alpha_dft);
                   // }

                 //   if (Math.Max(dens_solv.DFT_diff(carrier_density).Max(), (-1.0 * dens_solv.DFT_diff(carrier_density).Min())) < pot_lim)
                 //       converged = true;

                    // solution is converged if the density accuracy is better than half the minimum possible value for changing the dft potential
                    // also, check that the maximum change in the absolute value of the potential is less than a tolerance (default is 0.1meV)
                    if (dens_diff.Max() < min_dens_diff / 2.0 && current_vxc_diff < min_vxc_diff && t * x.InfinityNorm() < pot_diff_lim)
                        converged = true;
                }

                /*
                // Recalculate the charge density but for the updated potential rho(phi + t * x)
                bool edges_fine = false;
                while (!edges_fine)
                {
                    edges_fine = true;
                    SpinResolved_Data tmp_dens = dens_solv.Get_ChargeDensity(layers, carrier_density, dopent_density, chem_pot + t * x);
                    for (int i = 1; i < ny_dens - 1; i++)
                        for (int j = 1; j < nz_dens - 1; j++)
                            //if (i == 1 || j == 1 || i == ny_dens - 2 || j == nz_dens - 2)
                            if (i == 1 || i == ny_dens - 2)
                                if (Math.Abs(tmp_dens.Spin_Summed_Data.mat[i, j]) > edge_min_charge)
                                {
                                    if (x.mat.Max() > 0)
                                        t = 0.5 * t;
                                    else
                                        t = 2.0 * t;

                                    if (t > t_min)
                                    {
                                        edges_fine = false;
                                        goto end;
                                    }
                                    else
                                    {
                                        // although the edges are not fine at this point, we don't want the code to decrease t any further so we
                                        // break the loop by setting...
                                        edges_fine = true;
                                        goto end;
                                    }
                                }

                    if (!edges_fine)
                        throw new Exception("Error - Unable to reduce density to zero at edge of density domain.\nSimulation aborted");

                    end:
                    //if (t < t_damp * t_min)
                    //{
                    //    Console.WriteLine("Unable to reduce density to zero at edge of density domain\nRecalculating potential");
                    //    pois_solv.Set_Boundary_Conditions(top_V, split_V, split_width, bottom_V, surface_charge);
                    //    chem_pot = pois_solv.Get_Chemical_Potential(carrier_density.Spin_Summed_Data);
                    //    dens_solv.Get_ChargeDensity(layers, ref carrier_density, ref dopent_density, chem_pot);
                    //    Console.WriteLine("Potential recalculated");
                    //    edges_fine = true;
                    //}
                    //else
                        continue;
                }*/
                
                // update band energy phi_new = phi_old + t * x
  //              chem_pot = chem_pot + t * x;
                pois_solv.T = t;

                //// and set the DFT potential
                //if (count % 10 == 0)
                //    dens_solv.Print_DFT_diff(carrier_density);
                //dens_solv.Set_DFT_Potential(carrier_density);

                stpwch.Stop();
                Console.WriteLine("Iter = " + count.ToString() + "\tDens conv = " + dens_diff.Max().ToString("F4") + "\tt = " + t.ToString() + "\ttime = " + stpwch.Elapsed.TotalMinutes.ToString("F"));
                count++;

                // reset the potential if the added potential t * x is too small
                if (converged || count > max_count)
                {
                    Console.WriteLine("Maximum potential change at end of iteration was " + (t * x.InfinityNorm()).ToString());
                    break;
                }
            }

            Console.WriteLine("Iteration complete");
            return converged;
        }

        void Tmp_Print(Band_Data data, string file)
        {
            StreamWriter sw = new StreamWriter(file);
            for (int i = 0; i < ny_dens; i++)
                for (int j = 0; j < nz_dens; j++)
                {
                    sw.Write(data.mat[i, j].ToString() + '\t');
                    if (j == nz_dens - 1)
                        sw.WriteLine();
                }
            sw.Close();
        }

        double Get_FlexPDE_Data(string input_filename, string data_string)
        {
            string[] data = File.ReadAllLines(input_filename);

            for (int i = 0; i < data.Length; i++)
                if (data[i].Contains(data_string))
                    return double.Parse(data[i].Split(' ').Last());

            throw new KeyNotFoundException();
        }
    }
}
