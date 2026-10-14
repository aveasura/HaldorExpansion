using System;
using HaldorExpansion.Features.Upgrades;
using UnityEngine;
namespace UnityEngine {
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);}
 public struct Color {public float r,a;public Color(float r,float a=1){this.r=r;this.a=a;}}
 public static class Mathf {public static float Abs(float v)=>Math.Abs(v);public static int FloorToInt(float v)=>(int)Math.Floor(v);public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int Clamp(int v,int lo,int hi)=>Math.Min(hi,Math.Max(lo,v));}
}
class Tests {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS: "+name);}
 static Vector2 V(float x,float y)=>new Vector2(x,y);
 static void Main(){
  var atlas=new Color[64];for(int i=0;i<64;i++)atlas[i]=new Color(99);
  atlas[3*8+4]=new Color(1);atlas[3*8+5]=new Color(2,.5f);atlas[4*8+4]=new Color(3);atlas[4*8+5]=new Color(4);
  var verts=new[]{V(0,0),V(2,0),V(2,2),V(0,2)};var tri=new ushort[]{0,1,2,0,2,3};
  var uv=new[]{V(4f/8,3f/8),V(6f/8,3f/8),V(6f/8,5f/8),V(4f/8,5f/8)};
  var p=IdolIconRasterizer.Render(2,2,verts,tri,uv,atlas,8,8);
  Check(p[0].r==1 && p[1].r==2 && p[2].r==3 && p[3].r==4,"offset atlas region reconstructs full icon, not sprite.rect at atlas origin");
  Check(p[1].a==.5f,"source alpha preserved");
  var rotated=new[]{uv[1],uv[2],uv[3],uv[0]};p=IdolIconRasterizer.Render(2,2,verts,tri,rotated,atlas,8,8);
  Check(p[0].r==2 && p[1].r==4 && p[2].r==1 && p[3].r==3,"rotated packed UVs respected");
  p=IdolIconRasterizer.Render(2,2,verts,new ushort[]{2,1,0,3,2,0},uv,atlas,8,8);
  Check(p[0].r==1 && p[3].r==4,"reverse triangle winding works");
  var trimmed=new[]{V(1,1),V(3,1),V(3,3),V(1,3)};p=IdolIconRasterizer.Render(4,4,trimmed,tri,uv,atlas,8,8);
  Check(p[0].a==0 && p[15].a==0 && p[5].r==1 && p[10].r==4,"trimmed geometry retains original transparent margins and pivot placement");
  p=IdolIconRasterizer.Render(2,2,verts,new ushort[]{0,1,3},uv,atlas,8,8);
  Check(p[3].a==0 && p[0].a==1,"tight triangular mesh excludes neighboring atlas content");
  p=IdolIconRasterizer.Render(2,2,verts,new ushort[]{0,0,0},uv,atlas,8,8);
  Check(p[0].a==0 && p[3].a==0,"degenerate triangles safely skipped");
  Check(atlas[0].r==99 && atlas[3*8+4].r==1 && atlas[3*8+5].a==.5f,"source atlas untouched");
 }
}
