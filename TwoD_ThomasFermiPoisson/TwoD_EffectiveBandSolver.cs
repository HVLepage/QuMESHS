﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Solver_Bases;
using Solver_Bases.Layers;
using CenterSpace.NMath.Core;
using CenterSpace.NMath.Matrix;

namespace TwoD_ThomasFermiPoisson
{
    public class TwoD_EffectiveBandSolver : TwoD_Density_Base
    {
        DoubleVector energies;

        double no_kb_T = 50.0;          // number of kb_T to integrate to

        double tx, ty;

        public TwoD_EffectiveBandSolver(Experiment exp)
            : base(exp)
        {
            tx = -0.5 * Physics_Base.hbar * Physics_Base.hbar / (Physics_Base.mass * dx * dx);
            ty = -0.5 * Physics_Base.hbar * Physics_Base.hbar / (Physics_Base.mass * dy * dy);
        }

        public override void Get_ChargeDensity(ILayer[] layers, ref SpinResolved_Data charge_density, Band_Data chem_pot)
        {
            // convert the chemical potential into a quantum mechanical potential
            Band_Data dft_pot = chem_pot.DeepenThisCopy();
            Get_Potential(ref dft_pot, layers);

            DoubleHermitianEigDecomp eig_decomp = Solve_Eigenvector_Problem(dft_pot, ref charge_density);

            int max_wavefunction = (from val in eig_decomp.EigenValues
                                    where val < no_kb_T * Physics_Base.kB * temperature
                                    select val).ToArray().Length;

            double[] dens_x = new double[nx];
            // and generate a density for the y-direction
            for (int i = 0; i < nx; i++)
                for (int k = 0; k < max_wavefunction; k++)
                    dens_x[i] += DoubleComplex.Norm(eig_decomp.EigenVector(k)[i]) * DoubleComplex.Norm(eig_decomp.EigenVector(k)[i]) * Get_OneD_DoS(energies[k], no_kb_T);

            // multiply the z-densities by the y-density
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < ny; j++)
                {
                    charge_density.Spin_Up.mat[i, j] *= dens_x[i];
                    charge_density.Spin_Down.mat[i, j] *= dens_x[i];
                }
            
            // and multiply the density by -e to get the charge density (as these are electrons)
            charge_density = -1.0 * Physics_Base.q_e * charge_density;
        }

        public override void Get_ChargeDensity_Deriv(ILayer[] layers, ref SpinResolved_Data charge_density_deriv, Band_Data chem_pot)
        {
            // convert the chemical potential into a quantum mechanical potential
            Band_Data dft_pot = chem_pot.DeepenThisCopy();
            Get_Potential(ref dft_pot, layers);

            DoubleHermitianEigDecomp eig_decomp = Solve_Eigenvector_Problem(dft_pot, ref charge_density_deriv);

            int max_wavefunction = (from val in eig_decomp.EigenValues
                                    where val < no_kb_T * Physics_Base.kB * temperature
                                    select val).ToArray().Length;

            double[] dens_x_deriv = new double[nx];
            // and generate a density for the y-direction
            for (int i = 0; i < nx; i++)
                for (int k = 0; k < max_wavefunction; k++)
                    dens_x_deriv[i] += DoubleComplex.Norm(eig_decomp.EigenVector(k)[i]) * DoubleComplex.Norm(eig_decomp.EigenVector(k)[i]) * Get_OneD_DoS_Deriv(energies[k], no_kb_T);

            // multiply the z-densities by the y-density
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < ny; j++)
                {
                    charge_density_deriv.Spin_Up.mat[i, j] *= dens_x_deriv[i];
                    charge_density_deriv.Spin_Down.mat[i, j] *= dens_x_deriv[i];
                }

            // and multiply the density derivative by e to get the charge density (as increasing mu decreases the charge: dn/dmu*-e )
            charge_density_deriv = Physics_Base.q_e * charge_density_deriv;
        }

        /// <summary>
        /// returns an eigenvector decomposition for the x-direction after having calculated the density in the
        /// y-direction and storing it in "charge_density"
        /// </summary>
        /// <param name="dft_pot"></param>
        /// <param name="charge_density"></param>
        /// <returns></returns>
        private DoubleHermitianEigDecomp Solve_Eigenvector_Problem(Band_Data dft_pot, ref SpinResolved_Data charge_density)
        {
            DoubleHermitianEigDecomp eig_decomp;
            double[] x_energy = new double[nx];
            DoubleMatrix dens_up = new DoubleMatrix(nx, ny, 0.0);
            DoubleMatrix dens_down = new DoubleMatrix(nx, ny, 0.0);

            // cycle over x-direction calculating the ground state energy and eigenstate in the y-direction
            for (int i = 0; i < nx; i++)
            {
                // pull out the chemical potential for this slice
                double[] y_dft_pot = new double[ny];
                for (int j = 0; j < ny; j++)
                    y_dft_pot[j] = dft_pot.mat[i, j];

                // calculate its eigendecomposition
                DoubleHermitianMatrix h_y = Create_Hamiltonian(y_dft_pot, ty, ny);
                eig_decomp = new DoubleHermitianEigDecomp(h_y);

                // insert the eigenstate into density and record the local confinement energy
                x_energy[i] = eig_decomp.EigenValue(0);
                for (int j = 0; j < ny; j++)
                {
                    double dens_tot = DoubleComplex.Norm(eig_decomp.EigenVector(0)[j]) * DoubleComplex.Norm(eig_decomp.EigenVector(0)[j]);
                    dens_up[i, j] = 0.5 * dens_tot;
                    dens_down[i, j] = 0.5 * dens_tot;
                }
            }

            // calculate the eigenstates in the x-direction
            DoubleHermitianMatrix h_x = Create_Hamiltonian(x_energy, tx, nx);
            eig_decomp = new DoubleHermitianEigDecomp(h_x);
            energies = eig_decomp.EigenValues;

            // put the calculated densities into charge_density
            charge_density = new SpinResolved_Data(new Band_Data(dens_up), new Band_Data(dens_down));

            return eig_decomp;
        }

        /// <summary>
        /// returns the eigen-energies for the given potential and charge density
        /// </summary>
        public override DoubleVector Get_EnergyLevels(ILayer[] layers, Band_Data pot)
        {
            return energies;
        }
        
        DoubleHermitianMatrix Create_Hamiltonian(double[] V, double t, int N)
        {
            DoubleHermitianMatrix result = new DoubleHermitianMatrix(N);

            // set off diagonal elements 
            for (int i = 0; i < N; i++)
                {
                    // coupling sites in the growth direction
                    if (i != 0)
                        result[i - 1, i] = t;
                    if (i != N - 1)
                        result[i + 1, i] = t;
                }

            // set diagonal elements
            for (int i = 0; i < N; i++)
                result[i, i] = -2.0 * t + V[i];
                
            return result;
        }

        void Output_Wavefunctions(DoubleHermitianEigDecomp eig, int no_wavefunctions)
        {
            for (int i = 0; i < no_wavefunctions; i++)
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter("wav_" + i.ToString("00"));

                for (int j = 0; j < nx; j++)
                    sw.WriteLine((eig.EigenVector(i)[j].Real * Math.Sqrt(Get_OneD_DoS(eig.EigenValue(i), no_kb_T))).ToString());

                sw.Close();
            }
        }
    }
}
