#include "pch.h"

#include "Robert.h"
#include "algorithm_by_RF.h"
//#include "algorithm.h"

using namespace Robert; 

bool Interop::Compute(array<double>^ ir, array<double>^ r, double% spo2, double% bpm)
{
	unsigned int* irBuffer = new unsigned int[ir->Length]; 
	unsigned int* rBuffer = new unsigned int[r->Length]; 

	for (int c = 0; c < ir->Length; c++)
	{
		irBuffer[c] = ir[c]; 
		rBuffer[c] = r[c]; 
	}

	float tspo2; 
	char spo2Valid = 0, heartValid = 0; 
	int heartRate; 
	float ratio, coeff; 
	//maxim_heart_rate_and_oxygen_saturation(irBuffer, ir->Length, rBuffer, &tspo2, &spo2Valid, &heartRate, &heartValid); 
	rf_heart_rate_and_oxygen_saturation(irBuffer, ir->Length, rBuffer, &tspo2, &spo2Valid, &heartRate, &heartValid, &ratio, &coeff); 

	if (spo2Valid && heartValid)
	{
		spo2 = tspo2; 
		bpm = heartRate; 
		return true; 
	}
	else
	{
		return false; 
	}
}
