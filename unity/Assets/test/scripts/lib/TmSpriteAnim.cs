using UnityEngine;
using System.Collections;

[System.Serializable]
public class AnimAttribute{
	public int frame;
	public int flag;
}
[System.Serializable]
public class SpriteAnimation{
	public string name;
	public int[] frames;
	public AnimAttribute[] attrs;
	public bool loop;
	
	public SpriteAnimation(string animationName, int first, int last, bool loopPlayback){
		name = animationName;
		frames = new int[(last-first<1?1:last-first)];
		for(int ii=0; ii<frames.Length;++ii){ frames[ii]=ii+first; }
		loop = loopPlayback;
	}
}

// 簡易アニメーション
// アニメーション結果を自前Materialに保存またはメッシュを書き換え、
// 使いまわすことでDrawCallBatcingを適用させる 
public class TmSpriteAnim : MonoBehaviour {
	private const float ANIM_TIME_MIN = 0.0001f;
	public Material outMatreial;
	public Vector2 size;
	public Vector2 offset;
	public Vector2[] frames;
	public AnimAttribute[] frameAttrs;
	public SpriteAnimation[] animations;
	public string playOnAwake = "";
	public bool scaleAtUv = false;
	public bool setOnGrid = true;
	public bool reverse = false;
	public float fps = 20.0f;
	private bool mEnabled;
	private Vector2 mDefSize;
	private SpriteAnimation mCurrentAnm;
	private float mAnimPtr;
	private AnimAttribute mFrameAttr;
	private AnimAttribute mAnimAttr;
	private AnimAttribute mFrameAttrOld;
	private AnimAttribute mAnimAttrOld;
	private Vector2 mUvOfs;
	private Vector2 mUvPos;
	private Vector2 mTexSizeInv = Vector2.one;
	private Vector2[] mDefUvs = null;
	private Vector3[] mDefVtcs = null;
	private Material mTgetMat;
	private bool mIsEndOfFrame;
	public bool isPlay { get{ return mEnabled; } }
	public bool setOutMaterial(Material _mat){ outMatreial = _mat; return true; }
	public bool isEndFrame{ get{ return (mIsEndOfFrame); } }
	public void setUvOfs(Vector2 _uv){ mUvOfs = _uv; }
	public int frameFlag { get{ return (mFrameAttr!=null ? mFrameAttr.flag : 0); } }
	public int animFlag { get{ return (mAnimAttr!=null ? mAnimAttr.flag : 0); } }
	public int frameFragTrigger { get{ return ((mFrameAttrOld != mFrameAttr) ? frameFlag : 0); } }
	public int animFragTrigger { get{ return ((mAnimAttrOld != mAnimAttr) ? animFlag : 0); } }

	public Mesh getMesh(){
		Mesh ret = null;
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		if((meshFilter!=null)&&(meshFilter.mesh!=null)){
			ret = meshFilter.mesh;
		}
		return ret;
	}
	
	void Awake(){
		if(!this.enabled) return;
		
		mEnabled = true;
		mTgetMat = outMatreial!=null ? outMatreial : renderer!=null ? renderer.sharedMaterial : null;
		if((mTgetMat != null)&&(mTgetMat.mainTexture!=null)){
			mTexSizeInv = new  Vector2(1.0f/(float)(mTgetMat.mainTexture.width),1.0f/(float)(mTgetMat.mainTexture.height));
		}
		mDefSize = size;
		if(!scaleAtUv){
			mDefSize.Scale(mTexSizeInv);
		}
		Mesh nowMesh = getMesh();
		if(frames.Length>0){
			if(nowMesh==null){
				MeshFilter meshFilter = GetComponent<MeshFilter>();
				if(meshFilter==null){
					meshFilter = gameObject.AddComponent<MeshFilter>();
				}
				nowMesh = initMesh4(new Mesh());
				meshFilter.mesh = meshFilter.sharedMesh = nowMesh;
			}
			{
				Mesh sharedMesh = GetComponent<MeshFilter>().sharedMesh;
				mDefUvs = new Vector2[sharedMesh.vertexCount];
				mDefVtcs = new Vector3[sharedMesh.vertexCount];
				for(int ii = 0; ii < sharedMesh.vertexCount; ++ii){
					mDefUvs[ii] = new Vector2(sharedMesh.uv[ii].x,sharedMesh.uv[ii].y);
					mDefVtcs[ii] = new Vector3(sharedMesh.vertices[ii].x,sharedMesh.vertices[ii].y,sharedMesh.vertices[ii].z);
				}
			}
		}
		mFrameAttr = mFrameAttrOld = null;
		mAnimAttr = mAnimAttrOld = null;
		mCurrentAnm = null;
		mAnimPtr = 0.0f;
		mUvOfs = offset;
		mUvOfs.y *= -1.0f;
		if(setOnGrid){
			mUvOfs = Vector3.Scale(mUvOfs,size ); 
		}
		if(!scaleAtUv){
			mUvOfs.Scale(mTexSizeInv);
		}
		if(outMatreial!=null){
			Vector2 sz = size;
			if(!scaleAtUv){
				sz.x /= (float)(outMatreial.GetTexture("_MainTex").width);
				sz.y /= (float)(outMatreial.GetTexture("_MainTex").height);
			}
			outMatreial.SetTextureScale("_MainTex",sz); 
		}
		if(playOnAwake!=""){
			PlayAnimation(playOnAwake);
		}
	}
	
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		mFrameAttrOld = mFrameAttr;
		mAnimAttrOld = mAnimAttr;
		if((mCurrentAnm==null)||(mCurrentAnm.frames.Length<=1)) return;
		if(!mEnabled) return;
		
		float oldPtr = mAnimPtr;
		float animSpeed = Time.deltaTime*(fps<0.1f?0.1f:fps) * (!reverse?1.0f:-1.0f);
		mAnimPtr += animSpeed;
		if(!reverse){
			mIsEndOfFrame = ((mAnimPtr+animSpeed) > (float)mCurrentAnm.frames.Length);
		}else{
			mIsEndOfFrame = ((mAnimPtr+animSpeed) < 0.0f);
		}
		if(mCurrentAnm.loop){
			mAnimPtr = (mAnimPtr)%(float)mCurrentAnm.frames.Length;
			if(mAnimPtr<0.0f){
				mAnimPtr += (float)mCurrentAnm.frames.Length;
			}
		}else{
			if(!reverse){
				mAnimPtr = Mathf.Min(mAnimPtr,(float)mCurrentAnm.frames.Length-ANIM_TIME_MIN);
			}else{
				mAnimPtr = Mathf.Max(0.0f,mAnimPtr);
			}
		}
		if(Mathf.FloorToInt(oldPtr) != Mathf.FloorToInt(mAnimPtr)){
			updateAnim();
		}
		updateMesh();
	}

	public bool PlayAnimation(int _id){
		bool ret = false;
		if(_id < animations.Length){
			mEnabled = true;
			mCurrentAnm = animations[_id];
			mAnimPtr = 0.0f;
			mIsEndOfFrame = false;
			updateAnim();
			updateMesh();
			ret = true;
		}
		return ret;
	}
	public bool PlayAnimation(string _animName){
		bool ret = false;
		for(int ii = 0; ii < animations.Length; ++ii){
			if(animations[ii].name==_animName){
				ret = PlayAnimation(ii);
				break;
			}
		}
		return ret;
	}
	public void StopAnimation(){
		mEnabled = false;
	}
	
	public Mesh SetMeshColor(Color _col){
		Mesh nowMesh = getMesh();
		if(nowMesh!=null){
			Color[] cols = new Color[nowMesh.vertexCount];
			for(int ii = 0; ii < nowMesh.vertexCount; ++ii){
				cols[ii] = _col;
			}
			nowMesh.colors = cols;
		}
		return nowMesh;
	}

	public Mesh SetMeshScale(Vector3 _scale){
		Mesh nowMesh = getMesh();
		if(nowMesh!=null){
			Vector3[] scaleVecs = new Vector3[nowMesh.vertexCount];
			for(int ii = 0; ii < nowMesh.vertexCount; ++ii){
				scaleVecs[ii] = Vector3.Scale(getDefVertex(ii), _scale);
			}
			nowMesh.vertices = scaleVecs;
			nowMesh.RecalculateBounds ();
			nowMesh.Optimize();
		}
		return nowMesh;
	}
	
	public Mesh SetMeshUV(Vector2 _uvPos, Vector2 _size, bool _scaleAtUv=true){
		return setMeshUV(_uvPos, _size, _scaleAtUv);
	}
	
	private void updateAnim(){
		int animFrame = Mathf.FloorToInt(mAnimPtr);
		if((mCurrentAnm!=null)&&(animFrame < mCurrentAnm.frames.Length)){
			int viewFrame = mCurrentAnm.frames[animFrame];
			mUvPos = mUvOfs+getDefFrame(viewFrame);
			
			// attribute取得
			mFrameAttr = null;
			for( int ii = 0; ii < frameAttrs.Length; ++ii){
				if(frameAttrs[ii].frame==viewFrame){
					mFrameAttr = frameAttrs[ii];
					break;
				}
			}
			mAnimAttr = null;
			for( int ii = 0; ii < mCurrentAnm.attrs.Length; ++ii){
				if(mCurrentAnm.attrs[ii].frame==animFrame){
					mAnimAttr = mCurrentAnm.attrs[ii];
					break;
				}
			}
		}
	}

	private void updateMesh(){
		if(outMatreial!=null){
			outMatreial.SetTextureOffset("_MainTex",mUvPos);
		}else{
			setMeshUv();
		}
	}
	
	private Mesh setMeshUv(){
		return setMeshUV(mUvPos,size,scaleAtUv);
	}
	private Mesh setMeshUV(Vector2 _uvPos, Vector2 _size, bool _scaleAtUv){
		Mesh nowMesh = getMesh();
		if(nowMesh!=null){
			Vector2[] tmpUv = new Vector2[mDefUvs.Length];
			if(!_scaleAtUv){
				_size.Scale(mTexSizeInv);
			}
			for(int ii = 0; ii< mDefUvs.Length; ++ii){
				tmpUv[ii] = Vector2.Scale(mDefUvs[ii],_size) + _uvPos;
			}
			nowMesh.uv = tmpUv;
			nowMesh.Optimize();
		}
		return nowMesh;
	}

	
	private Vector2 getDefFrame(int _frameId){
		Vector2 defFrame = frames[_frameId];
		if(setOnGrid){
			defFrame = Vector3.Scale(defFrame,size ); 
		}
		if(!scaleAtUv){
			Material tgetMat = outMatreial!=null ? outMatreial : renderer.sharedMaterial;
			Vector2 texSizeInv = new  Vector2(1.0f/(float)(tgetMat.mainTexture.width),1.0f/(float)(tgetMat.mainTexture.height));
			defFrame.Scale(texSizeInv);
		}
		defFrame.y = 1.0f-defFrame.y-mDefSize.y;
		return defFrame;
	}
	private Vector3 getDefVertex(int _vtxId){
//		return(GetComponent<MeshFilter>().sharedMesh.vertices[_vtxId]);
		return mDefVtcs[_vtxId];
	}
	
	private Mesh initMesh4(Mesh _mesh){
		_mesh.vertices = new Vector3[]{
			new Vector3 (-0.5f, 0.5f, 0.0f),
			new Vector3 (0.5f, 0.5f, 0.0f),
			new Vector3 (0.5f, -0.5f, 0.0f),
			new Vector3 (-0.5f, -0.5f, 0.0f)
		};
		_mesh.triangles = new int[]{ 0, 1, 2, 2, 3, 0 };
		_mesh.uv = new Vector2[]{
			new Vector2 (0.0f, 1.0f),
			new Vector2 (1.0f, 1.0f),
			new Vector2 (1.0f, 0.0f),
			new Vector2 (0.0f, 0.0f)
		};
		_mesh.colors = new Color[]{
			new Color(0.5f,0.5f,0.5f,1.0f),
			new Color(0.5f,0.5f,0.5f,1.0f),
			new Color(0.5f,0.5f,0.5f,1.0f),
			new Color(0.5f,0.5f,0.5f,1.0f)
		};
		_mesh.normals = new Vector3[]{
			new Vector3 (0.0f, 0.0f, 1.0f),
			new Vector3 (0.0f, 0.0f, 1.0f),
			new Vector3 (0.0f, 0.0f, 1.0f),
			new Vector3 (0.0f, 0.0f, 1.0f)
		};
		_mesh.RecalculateNormals ();
		_mesh.RecalculateBounds ();
		_mesh.Optimize();
		return _mesh;
	}

}
