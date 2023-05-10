#include "AttributeManager.hpp"


AttributeManager* AttributeManager::instance = nullptr;

AttributeManager* AttributeManager::GetInstance ()
{
	if (nullptr == instance) {
		instance = new AttributeManager;
	}
	return instance;
}


void AttributeManager::DeleteInstance ()
{
	if (nullptr != instance) {
		delete instance;
		instance = nullptr;
	}
}


AttributeManager::AttributeManager () {}


GSErrCode AttributeManager::GetMaterial (const ModelInfo::Material& material, API_Attribute& attribute)
{
	GS::UniString materialName (material.GetName ());
	char typeId = API_MaterialID;
	GS::UniString key = materialName + typeId;

	if (cache.Get (key, &attribute)) {
		return NoError;
	} else {
		BNZeroMemory (&attribute, sizeof (API_Attribute));

		attribute.header.uniStringNamePtr = &materialName;
		attribute.header.typeID = API_MaterialID;

		GSErrCode err = ACAPI_Attribute_Get (&attribute);
		if (NoError == err) {
			cache.Add (key, attribute);
		}

		if (APIERR_BADNAME == err) {
			attribute.material.mtype = APIMater_GeneralID;
			attribute.material.ambientPc = 100;
			attribute.material.diffusePc = 0;
			attribute.material.specularPc = 0;

			attribute.material.transpPc = material.GetTransparency ();
			attribute.material.shine = 50;
			attribute.material.transpAtt = 400;
			attribute.material.emissionAtt = 0;

			attribute.material.surfaceRGB.f_red = material.GetAmbientColor ().red / 65535.0;
			attribute.material.surfaceRGB.f_green = material.GetAmbientColor ().green / 65535.0;
			attribute.material.surfaceRGB.f_blue = material.GetAmbientColor ().blue / 65535.0;

			attribute.material.specularRGB.f_red = .0;
			attribute.material.specularRGB.f_green = .0;
			attribute.material.specularRGB.f_blue = .0;

			attribute.material.emissionRGB.f_red = material.GetEmissionColor ().red / 65535.0;
			attribute.material.emissionRGB.f_green = material.GetEmissionColor ().green / 65535.0;
			attribute.material.emissionRGB.f_blue = material.GetEmissionColor ().blue / 65535.0;

			API_Attribute fill;
			GS::UniString fillName;
			GetDefaultFill (fill, fillName);
			attribute.material.ifill = fill.header.index;

			err = ACAPI_Attribute_Create (&attribute, 0);
			if (NoError == err) {
				cache.Add (key, attribute);
			}
		}

		return err;
	}
}


GSErrCode AttributeManager::GetFill (GS::UniString& fillName, API_Attribute& attribute)
{
	char typeId = API_FilltypeID;
	GS::UniString key = fillName + typeId;

	if (cache.Get (key, &attribute)) {
		return NoError;
	} else {
		BNZeroMemory (&attribute, sizeof (API_Attribute));

		attribute.header.uniStringNamePtr = &fillName;
		attribute.header.typeID = API_FilltypeID;

		GSErrCode err = ACAPI_Attribute_Get (&attribute);
		if (NoError == err) {
			cache.Add (key, attribute);
		}

		if (APIERR_BADNAME == err) {
			attribute.header.flags = APIFill_ScaleWithPlan | APIFill_ForPoly | APIFill_ForCover;

			attribute.filltype.subType = APIFill_Symbol;
			attribute.filltype.hXSpac = 1.0;
			attribute.filltype.hYSpac = 1.0;

			err = ACAPI_Attribute_Create (&attribute, 0);
			if (NoError == err) {
				cache.Add (key, attribute);
			}
		}

		return err;
	}
}


GSErrCode AttributeManager::GetDefaultMaterial (API_Attribute& attribute, GS::UniString& name)
{
	GS_RGBColor	ambientColor;
	ambientColor.red = 0x7f * 256;
	ambientColor.green = 0x7f * 256;
	ambientColor.blue = 0x7f * 256;

	GS_RGBColor	emissionColor;
	emissionColor.red = 0x0;
	emissionColor.green = 0x0;
	emissionColor.blue = 0x0;

	name = "Default Speckle Surface";
	ModelInfo::Material material (name, 0, ambientColor, emissionColor);

	return GetMaterial (material, attribute);
}


GSErrCode AttributeManager::GetDefaultFill (API_Attribute& attribute, GS::UniString& name)
{
	name = "Default Speckle Fill";

	return GetFill (name, attribute);
}