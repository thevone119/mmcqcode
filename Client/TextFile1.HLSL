//
// 全局变量
//
sampler BaseTex;
 
//
// input structures(输入结构)
//
struct PS_INPUT
{
   float2 base      : TEXCOORD;
};
//
// output structures(输出结构)
//
struct PS_OUTPUT
{
    vector color : COLOR;
};
 
//
//  Main Function
//
PS_OUTPUT Main(PS_INPUT input)
{
    // zero out members of output
    PS_OUTPUT output = (PS_OUTPUT)0;
	
    vector b = tex2D(BaseTex,      input.base);
 
	//核心代码，学过图形学的应该知道，对一个像素的颜色进行重新计算
    vector c = 0.3 * b.x + 0.59 * b.y + 0.11 * b.z ;
	
    output.color = c;
 
    return output;
}
