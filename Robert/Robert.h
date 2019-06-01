#pragma once

using namespace System;

namespace Robert {
	public ref class Interop
	{
	public:
		static bool Compute(array<double>^ ir, array<double>^ r, double% spo2, double% bpm, double% xyRatio);
	};
}
