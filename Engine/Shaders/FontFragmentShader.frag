#version 330 

in vec2 passTextureCoords;

out vec4 outColor;

uniform vec3 color;
uniform sampler2D textureAtlas;
//cambiare le constanti in uniforms

//aumentare con font grandi
const float width = 0.5;
//diminuire con font piccoli
const float edge = 0.1;
const float borderwidth = 0.7;
const float borderEdge = 0.1;
//ombra
const vec2 offset = vec2(0.0, 0.0);
//colore esterno
const vec3 outlineColor = vec3(0.2, 0.2, 0.2);

void main(void)
{
	float dst = 1.0 - texture(textureAtlas, passTextureCoords).a;
	float alpha = 1.0 - smoothstep(width, width + edge, dst);
	
	float dst2 = 1.0 - texture(textureAtlas, passTextureCoords + offset).a;
	float outlineAlpha = 1.0 - smoothstep(borderwidth, borderwidth + edge, dst2);
	
	float overallAlpha = alpha + (1.0 - alpha) * outlineAlpha;
	vec3 overallColor = mix(outlineColor, color, alpha / overallAlpha);
	
	outColor = vec4(overallColor, overallAlpha);
}