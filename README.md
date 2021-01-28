<!-- Header -->
<br />
<p align="center">

  <h3 align="center">Euclidean Raytracing</h3>

  <p align="center">
    A Unity project that implements from scratch GPU raytracing. A Building block for a future non-euclidean raytracer. This project is roughly built off of code following the blog posts at <a href="http://three-eyed-games.com/2018/05/03/gpu-ray-tracing-in-unity-part-1/">Three eyed games</a>. Version 1.0 was written closely following those posts, everything after is more free form/ original.
  </p>
</p>

<p align="center"><img src="Images/Converged.gif" width="900"/>
<img src="Images/Screenshot1.png"/>
<img src="Images/Screenshot2.png"/>
  
<img src="Images/Screenshot3.png"/>

# How it works

all of the rendering is handled by `RayTracingMaster.cs` on the CPU and `RayTracingShader.compute` on the GPU. `RayTracingShader.compute` runs for each pixel and determines the color using the ray tracing algorithm. My goal with this section is to formalize the mathematics of my code for easier adaptation of differential geometry/ noneuclidean techniques later on down the line.

## Theory
A ray object, <img src="svgs/e2acc9a5afbaa3b37bede0610b46591e.svg" align=middle width=10.82192594999999pt height=23.488575000000026pt/>, is generated, with an origin and direction. The initial ray corresponds to a random point in pixel space (`AddShader.shader` averages over multiple frames, which smoothes out the edges effectively anti-aliasing). The `Trace` function loops through every object and checks analytically whether the ray intersects the object. Included are algorithms for intersections with [planes](https://en.wikipedia.org/wiki/Line%E2%80%93plane_intersection#:~:text=In%20analytic%20geometry%2C%20the%20intersection,the%20plane%20but%20outside%20it.), [spheres](https://en.wikipedia.org/wiki/Line%E2%80%93sphere_intersection), and [triangles](http://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/pubs/raytri_tam.pdf). The `RayTracingObject.cs` is assigned to Unity objects in the scene, it tells the shader to check intersections with any mesh triangles in the object. Of all the intersections, the closest is chosen and the `Shade` function is called to calculate the pixel color. If no intersections are found then the color is chosen from an HDRI image (the skybox).

The luminance (color) at an intersection point is given by the formula

<p align="center"><img src="svgs/59bae2c7c550ade8d7814aa1021b7026.svg" align=middle width=414.62049255pt height=37.3519608pt/></p> 



That is, the color of light at a point is the sum of the light emitted at that point and the integral of all the light hitting that point. The integral is performed over the hemisphere of directions pointing away from the surface, formally <p align="center"><img src="svgs/6978d478769308b47b015fddc1e550ab.svg" align=middle width=190.1003742pt height=18.312383099999998pt/></p> 

<img src="svgs/ac6f4c28fa910f0acc036b6cbacfcdc1.svg" align=middle width=85.43096924999998pt height=24.65753399999998pt/> is called the BRDF (bidirectional reflectance distribution function) which is tied to the material properties of an object. Notice that <img src="svgs/4303371fb671cf430e385080f0836728.svg" align=middle width=51.49543079999999pt height=24.65753399999998pt/> appears on both sides of the equation, this is because of the recursive nature of raytracing. For each surface that a ray bounces off of, this integeral is performed.

### Computing the Integral
In general, the integral of a function <img src="svgs/3a62668063552dc0a27a84c0e705f2df.svg" align=middle width=127.00483454999997pt height=24.65753399999998pt/> over <img src="svgs/9432d83304c1eb0dcb05f092d30a767f.svg" align=middle width=11.87217899999999pt height=22.465723500000017pt/> is just <img src="svgs/5a7b63fcb316fdefe42e319d18ab939a.svg" align=middle width=18.179315549999988pt height=21.18721440000001pt/> times the average of the function
<p align="center"><img src="svgs/a6531303106405fdbf039f498a2e39f1.svg" align=middle width=180.28647615pt height=37.3519608pt/></p>

Care must be taken with how the rays are chosen, the above equation assumes that rays are sampled using a uniform distribuiton over the hemisphere. The problem is that for our light integral some reflected rays are much more probable than others, and we would be wasting a lot of GPU time calculating rays that don't contribute much. If we sample rays according to a probability distribution function <img src="svgs/7adf54a3cecf75e8d5af0793614411d5.svg" align=middle width=36.98922314999999pt height=24.65753399999998pt/>, then our average becomes
<p align="center"><img src="svgs/f745e5cc4e501bc50e54d9bd347c0ee8.svg" align=middle width=180.17852489999999pt height=42.511787999999996pt/></p>
this is known as importance sampling. Ideally we would sample rays with a probabilty proportional to the BRDF, but some BRDFs may be too complex to reasonably use for sampling.

### Cosine sampling
The general probability distribution that is most useful to us is
<p align="center"><img src="svgs/7a1437b0971a826da817fdf0cd775a57.svg" align=middle width=173.9609553pt height=32.990165999999995pt/></p>

This is a radial lobe, with a peak in the direction of <img src="svgs/cd74c822d31d457e590f28706c11499d.svg" align=middle width=10.747741949999996pt height=23.488575000000026pt/> and a strength corresponding to increasing values of <img src="svgs/c745b9b57c145ec5577b82542b2df546.svg" align=middle width=10.57650494999999pt height=14.15524440000002pt/>. We also only care about hemispheres so we restrict <img src="svgs/556e9f18f8681cebda959cd52b4e67c4.svg" align=middle width=66.2715504pt height=23.488575000000026pt/>. <img src="svgs/1924b0e737a1c5c085f6e7f1b0fa4840.svg" align=middle width=40.713337499999994pt height=21.18721440000001pt/> corresponds to the usual uniform distribution on the hemisphere. Random rays are sampled using this distribution in the funtion `SampleHemisphere`. The method of accomplishing this in code comes from [this](https://blog.thomaspoulet.fr/uniform-sampling-on-unit-hemisphere/) blog.

### BRDFs
When a ray hits a surface there are currently two models for how it will reflect, [diffuse](https://en.wikipedia.org/wiki/Diffuse_reflection) and [specular](https://en.wikipedia.org/wiki/Specular_reflection). 

Diffuse shading is modeled using [Lambertian reflectance](https://en.wikipedia.org/wiki/Lambertian_reflectance), with a BRDF
<p align="center"><img src="svgs/38f054158637a3f43602ffbf2d4c997f.svg" align=middle width=230.61174014999997pt height=32.44222905pt/></p>

A convenient choice of sampling would be <img src="svgs/0932fdb96bc9e2955f5c829e4a8748bc.svg" align=middle width=131.02144439999998pt height=27.77565449999998pt/> (i.e. <img src="svgs/c9dbc3793c46e3142103f06476da99df.svg" align=middle width=40.713337499999994pt height=21.18721440000001pt/>). Putting this all together, the luminance from a diffuse reflection is calculated as
<p align="center"><img src="svgs/253b546613c65d4435c0bda849c7f700.svg" align=middle width=386.60433075pt height=17.031940199999998pt/></p>

Specular shading is modeled using [Phong reflection](https://en.wikipedia.org/wiki/Phong_reflection_model) (I also wrote currently unimplemented code for the more physically accurate but computationally expensive [Blinn-Phong model](https://en.wikipedia.org/wiki/Blinn%E2%80%93Phong_reflection_model)). The Phong BRDF is
<p align="center"><img src="svgs/d6718a2ddf7d3da6cceecefe81aa3cf9.svg" align=middle width=365.2575366pt height=32.990165999999995pt/></p>

where <img src="svgs/0b09db718fca1e696839231403f23259.svg" align=middle width=16.68957839999999pt height=23.488575000000026pt/> is the reflected ray <img src="svgs/a5174f524836120f6af1bdd08cbf81ed.svg" align=middle width=147.7842267pt height=24.65753399999998pt/>. And <img src="svgs/3abe2ed67c5e54bd223f9cfdeb3288fa.svg" align=middle width=174.88717784999997pt height=32.44583099999998pt/>. The Phong BRDF is always proportional to our cosine weighted sampling distribution, so we can simplify the luminance for specular reflections as
<p align="center"><img src="svgs/2cd380219d00c1c0db044fa5c042b357.svg" align=middle width=498.83875965pt height=34.3600389pt/></p>

All materials are some combination of diffuse and specular reflections (there is also refraction which has not been implemented yet). When a ray hits a surface there is a probability for each method of reflection. We can compute this probability using the color channel averages of the `albedo` and `specular` values, for example the probability that a ray will reflect diffusely is <img src="svgs/730c17d055d79c5bb5a65240adb404b6.svg" align=middle width=95.67471869999999pt height=27.00852000000001pt/>. We will need to multiply the probability of diffuse reflection by the probability that the ray will reflect in a certain direction given diffuse reflection to get the total probability needed for importance sampling (bayes theorem). Finally we store <img src="svgs/513cbb7d5396b42e6ce340bf112c3045.svg" align=middle width=64.51703444999998pt height=24.65753399999998pt/> as a single variable. Putting this all together, the total luminance calculated in the `shade` function is
<p align="center"><img src="svgs/cdd3d33de99c446208a1c54a01ce8049.svg" align=middle width=567.58090125pt height=104.1356976pt/></p>

# TODO
- [ ] Texture mapping/buffering
- [ ] Depth of Field/ physical camera
- [ ] UI using Dear ImGui
- [ ] Optimization of the `Trace` function using BVHs or sparse voxel octrees
