﻿using SiaNet.Data;
using System;
using System.Linq;
using Deedle;
using SiaNet;
using SiaNet.Layers;
using SiaNet.Regularizers;
using TensorSharp;

namespace BasicClassificationWithTitanicDataset
{
    class Program
    {
        static void Main(string[] args)
        {
            Global.UseGpu();
            var dataset = LoadTrain(); //Load train data
            var test = LoadTest(); //Load test data

            var (train, val) = dataset.Split(0.25);

            var model = new Sequential();
            model.EpochEnd += Model_EpochEnd;
            model.Add(new Dense(64, ActType.ReLU));
            model.Add(new Dense(32, ActType.ReLU));
            model.Add(new Dense(1, ActType.Sigmoid));

            //Compile with Optimizer, Loss and Metric
            model.Compile(OptimizerType.Adam, LossType.BinaryCrossEntropy, MetricType.BinaryAccurary);

            // Train for 100 epoch with batch size of 2
            model.Train(train, 100, 32, val);

            //var prediction = model.Predict(test);
            //TOps.Round(prediction).Print();
        }

        private static void Model_EpochEnd(object sender, EpochEndEventArgs e)
        {
            Console.WriteLine("Epoch: {0}, Loss: {1}, Acc: {2}, Val_Loss: {3}, Val_Acc: {4}", e.Epoch, e.Loss, e.Metric, e.ValidationLoss, e.ValidationMetric);
        }

        private static DataFrameIter LoadTrain()
        {
            //Using deedle which is similar to Pandas in python
            var frame = Frame.ReadCsv("train.csv", true);

            //Preprocess the data by handling missing values, converting string to numbers
            frame = PreProcesData(frame);

            //Load Deedle frame to Tensor frame
            var data = frame.ToArray2D<float>().Cast<float>().ToArray();
            DataFrame2D df = new DataFrame2D(frame.ColumnCount);
            df.Load(data);

            //Split X and Y
            var x = df[0, 6];
            var y = df[7];

            return new DataFrameIter(x, y);
        }

        private static DataFrame LoadTest()
        {
            //Using deedle which is similar to Pandas in python
            var frame = Frame.ReadCsv("test.csv", true);

            //Preprocess the data by handling missing values, converting string to numbers
            frame = PreProcesData(frame, true);

            //Load Deedle frame to Tensor frame
            var data = frame.ToArray2D<float>().Cast<float>().ToArray();
            DataFrame2D df = new DataFrame2D(frame.ColumnCount);
            df.Load(data);

            return df;
        }

        private static Frame<int, string> PreProcesData(Frame<int, string> frame, bool isTest = false)
        {
            // Drop some colmuns which will not help in prediction
            frame.DropColumn("PassengerId");
            frame.DropColumn("Name");
            frame.DropColumn("Ticket");
            frame.DropColumn("Cabin");

            //Fill missing data with nearest values
            frame = frame.FillMissing(Direction.Forward);

            // Convert male/female to 1/0
            frame["Sex"] = frame.GetColumn<string>("Sex").SelectValues<double>((x) => {
                return x == "male" ? 1 : 0;
            });

            // Convert S/C/Q -> 0/1/2
            frame["Embarked"] = frame.GetColumn<string>("Embarked").SelectValues<double>((x) => {
                if (x == "S")
                    return 0;
                else if (x == "C")
                    return 1;
                else if (x == "Q")
                    return 2;

                return 0;
            });

            // Convert Survived to 1 or 0
            if(!isTest)
                frame["Survived"] = frame.GetColumn<bool>("Survived").SelectValues<double>((x) => {
                    return x ? 1 : 0;
                });

            return frame;
        }
    }
}