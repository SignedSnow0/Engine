#version 400 core

in vec2 passTextureCoords;
in vec3 surfaceNormal;
in vec3 toLightVector[4];
in vec3 toCameraVector;
in float visibility;

out vec4 color;

uniform sampler2D backgroundTexture;
uniform sampler2D rTexture;
uniform sampler2D gTexture;
uniform sampler2D bTexture;
uniform sampler2D blendMap;

uniform vec3 lightColor[4];
uniform vec3 attenuation[4];
uniform float shineDamper;
uniform float reflectivity;
uniform vec3 skyColor;

void main(void)
{
	vec4 blendMapColour = texture(blendMap, passTextureCoords);
	
	float backTextureAmount = 1 - (blendMapColour.r + blendMapColour.g + blendMapColour.b);
	vec2 tiledCoords = passTextureCoords * 30.0;
	vec4 backgroundTextureColour = texture(backgroundTexture, tiledCoords) * backTextureAmount;
	vec4 rTextureColour = texture(rTexture,tiledCoords) * blendMapColour.r;
	vec4 gTextureColour = texture(gTexture,tiledCoords) * blendMapColour.g;
	vec4 bTextureColour = texture(bTexture,tiledCoords) * blendMapColour.b;
	
	vec4 totalColor = backgroundTextureColour + rTextureColour + gTextureColour + bTextureColour;

	vec3 unitNormal = normalize(surfaceNormal);
	vec3 unitVectorToCamera = normalize(toCameraVector);
	
	vec3 totalDiffuse = vec3(0.0);
	vec3 totalSpecular = vec3(0.0);
	
	for(int i = 0; i < 4; i++)
	{
		float distance = length(toLightVector[i]);
		float attFactor = attenuation[i].x + (attenuation[i].y * distance) + (attenuation[i].z * distance * distance);
		vec3 unitLightVector = normalize(toLightVector[i]);	
		float nDotl = dot(unitNormal,unitLightVector);
		float brightness = max(nDotl,0.0);
		vec3 lightDirection = -unitLightVector;
		vec3 reflectedLightDirection = reflect(lightDirection,unitNormal);
		float specularFactor = dot(reflectedLightDirection , unitVectorToCamera);
		specularFactor = max(specularFactor,0.0);
		float dampedFactor = pow(specularFactor,shineDamper);
		totalDiffuse = totalDiffuse + (brightness * lightColor[i])/attFactor;
		totalSpecular = totalSpecular + (dampedFactor * reflectivity * lightColor[i])/attFactor;
	}
	totalDiffuse = max(totalDiffuse, 0.2);

	color =  vec4(totalDiffuse,1.0) * totalColor + vec4(totalSpecular,1.0);
	color = mix(vec4(skyColor,1.0),color, visibility);
}