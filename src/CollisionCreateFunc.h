#pragma once

#include "IDisposable.h"

namespace BulletSharp
{
	ref class CollisionAlgorithm;
	ref class CollisionAlgorithmConstructionInfo;
	ref class CollisionObjectWrapper;

	public ref class CollisionAlgorithmCreateFunc
	{
	internal:
		btCollisionAlgorithmCreateFunc* _native;
		CollisionAlgorithmCreateFunc(btCollisionAlgorithmCreateFunc* native);

	private:
		bool _deleteObject;

	public:
		!CollisionAlgorithmCreateFunc();
	protected:
		~CollisionAlgorithmCreateFunc();

	public:
		CollisionAlgorithmCreateFunc();

		CollisionAlgorithm^ CreateCollisionAlgorithm(CollisionAlgorithmConstructionInfo^ info,
			CollisionObjectWrapper^ body0Wrap, CollisionObjectWrapper^ body1Wrap);

		property bool IsDisposed
		{
			virtual bool get();
		}

		property bool Swapped
		{
			bool get();
			void set(bool value);
		}
	};
};
