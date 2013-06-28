﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CenterSpace.NMath.Core;

namespace Solver_Bases
{    
    /// <summary>
    /// A class for holding spin resolved DoubleVector classes from CenterSpace types
    /// This is just a pair of DoubleVectors which can be indexed
    /// </summary>
    public class SpinResolved_DoubleVector
    {
        DoubleVector[] spin_vector;
        int nx;

        public SpinResolved_DoubleVector(int nx)
        {
            this.nx = nx;
            spin_vector = new DoubleVector[2];

            spin_vector[0] = new DoubleVector(nx);
            spin_vector[1] = new DoubleVector(nx);
        }

        public double this[int i, Spin spin]
        {
            get { return this.Spin_Vector(spin)[i]; }
            set { this.Spin_Vector(spin)[i] = value; }
        }

        /// <summary>
        /// Cast from DoubleVector to SpinResolved_DoubleMatrix
        /// Assumes equal spin contributions for up and down in mat
        /// </summary>
        public static explicit operator SpinResolved_DoubleVector(DoubleVector vec)
        {
            SpinResolved_DoubleVector result = new SpinResolved_DoubleVector(vec.Length);
            result.Spin_Up = vec / 2.0;
            result.Spin_Down = vec / 2.0;

            return result;
        }

        public static SpinResolved_DoubleVector operator *(double scalar, SpinResolved_DoubleVector vec)
        {
            vec.Spin_Up *= scalar; vec.Spin_Down *= scalar;

            return vec;
        }

        public static SpinResolved_DoubleVector operator *(SpinResolved_DoubleVector vec, double scalar)
        {
            return scalar * vec;
        }


        public static SpinResolved_DoubleVector operator +(SpinResolved_DoubleVector vec_1, SpinResolved_DoubleVector vec_2)
        {
            // check if lengths are the same
            if (vec_1.Nx != vec_2.Nx)
                throw new ArrayTypeMismatchException();

            SpinResolved_DoubleVector result = new SpinResolved_DoubleVector(vec_1.Nx);
            result.Spin_Up = vec_1.Spin_Up + vec_2.Spin_Up;
            result.Spin_Down = vec_1.Spin_Down + vec_2.Spin_Down;

            return result;
        }

        public static SpinResolved_DoubleVector operator -(SpinResolved_DoubleVector vec_1, SpinResolved_DoubleVector vec_2)
        {
            // check if lengths are the same
            if (vec_1.Nx != vec_2.Nx)
                throw new ArrayTypeMismatchException();

            SpinResolved_DoubleVector result = new SpinResolved_DoubleVector(vec_1.Nx);
            result.Spin_Up = vec_1.Spin_Up - vec_2.Spin_Up;
            result.Spin_Down = vec_1.Spin_Down - vec_2.Spin_Down;

            return result;
        }

        /// <summary>
        /// returns the DoubleMatrix for the given spin
        /// </summary>
        public DoubleVector Spin_Vector(Spin spin)
        {
            if (spin == Spin.Up)
                return spin_vector[0];
            else
                return spin_vector[1];
        }

        public DoubleVector Spin_Summed_Vector
        {
            get { return spin_vector[0] + spin_vector[1]; }
        }

        public int Nx
        {
            get { return nx; }
        }

        public DoubleVector Spin_Up
        {
            get { return spin_vector[0]; }
            set { spin_vector[0] = value; }
        }

        public DoubleVector Spin_Down
        {
            get { return spin_vector[1]; }
            set { spin_vector[1] = value; }
        }
    }


    /// <summary>
    /// A class for holding spin resolved DoubleMatrix classes from CenterSpace types
    /// This is just a pair of DoubleMatrices which can be indexed
    /// </summary>
    public class SpinResolved_DoubleMatrix
    {
        int nx, ny;
        DoubleMatrix[] spin_matrix;

        public SpinResolved_DoubleMatrix(int nx, int ny)
        {
            this.nx = nx; this.ny = ny;
            spin_matrix = new DoubleMatrix[2];

            spin_matrix[0] = new DoubleMatrix(nx, ny);
            spin_matrix[1] = new DoubleMatrix(nx, ny);
        }

        public double this[int i, int j, Spin spin]
        {
            get { return this.Spin_Matrix(spin)[i, j]; }
            set { this.Spin_Matrix(spin)[i, j] = value; }
        }

        /// <summary>
        /// Cast from DoubleMatrix to SpinResolved_DoubleMatrix
        /// Assumes equal spin contributions for up and down in mat
        /// </summary>
        public static explicit operator SpinResolved_DoubleMatrix(DoubleMatrix mat)
        {
            SpinResolved_DoubleMatrix result = new SpinResolved_DoubleMatrix(mat.Rows, mat.Cols);
            result.Spin_Up = mat / 2.0;
            result.Spin_Down = mat / 2.0;

            return result;
        }

        public static SpinResolved_DoubleMatrix operator *(double scalar, SpinResolved_DoubleMatrix mat)
        {
            mat.Spin_Up *= scalar; mat.Spin_Down *= scalar;

            return mat;
        }

        public static SpinResolved_DoubleMatrix operator *(SpinResolved_DoubleMatrix mat, double scalar)
        {
            return scalar * mat;
        }

        public static SpinResolved_DoubleMatrix operator +(SpinResolved_DoubleMatrix mat_1, SpinResolved_DoubleMatrix mat_2)
        {
            // check if lengths are the same
            if (mat_1.Nx != mat_2.Nx || mat_1.Ny != mat_2.Ny)
                throw new ArrayTypeMismatchException();

            SpinResolved_DoubleMatrix result = new SpinResolved_DoubleMatrix(mat_1.Nx, mat_1.Ny);
            result.Spin_Up = mat_1.Spin_Up + mat_2.Spin_Up;
            result.Spin_Down = mat_1.Spin_Down + mat_2.Spin_Down;

            return result;
        }

        public static SpinResolved_DoubleMatrix operator -(SpinResolved_DoubleMatrix mat_1, SpinResolved_DoubleMatrix mat_2)
        {
            // check if lengths are the same
            if (mat_1.Nx != mat_2.Nx || mat_1.Ny != mat_2.Ny)
                throw new ArrayTypeMismatchException();

            SpinResolved_DoubleMatrix result = new SpinResolved_DoubleMatrix(mat_1.Nx, mat_1.Ny);
            result.Spin_Up = mat_1.Spin_Up - mat_2.Spin_Up;
            result.Spin_Down = mat_1.Spin_Down - mat_2.Spin_Down;

            return result;
        }

        /// <summary>
        /// returns the DoubleMatrix for the given spin
        /// </summary>
        public DoubleMatrix Spin_Matrix(Spin spin)
        {
            if (spin == Spin.Up)
                return spin_matrix[0];
            else
                return spin_matrix[1];
        }

        public DoubleMatrix Spin_Summed_Matrix
        {
            get { return spin_matrix[0] + spin_matrix[1]; }
        }

        public int Nx
        {
            get { return nx; }
        }

        public int Ny
        {
            get { return ny; }
        }

        public DoubleMatrix Spin_Up
        {
            get { return spin_matrix[0]; }
            set { spin_matrix[0] = value; }
        }

        public DoubleMatrix Spin_Down
        {
            get { return spin_matrix[1]; }
            set { spin_matrix[1] = value; }
        }
    }
}