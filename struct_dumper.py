import io
from re import I
import sys
from typing import cast
from zipfile import ZipFile
from UnityPy import Environment
from UnityPy.classes import MonoBehaviour

env = Environment()

with ZipFile(sys.argv[1]) as ipa:
    with ipa.open("Payload/Phigros.app/Data/globalgamemanagers.assets") as f:
        env.load_file(io.BytesIO(f.read()), name="Payload/Phigros.app/Data/globalgamemanagers.assets")
    
    with ipa.open("Payload/Phigros.app/Data/level0") as f:
        env.load_file(io.BytesIO(f.read()))

for obj in env.objects:
    if obj.type.name != "MonoBehaviour":
        continue

    data = cast(MonoBehaviour, obj.read())
    
    if (obj := data.m_Script.get_obj()) is None:
        continue
    
    objName = obj.read().name 

    if objName in {"GameInformation", "GetCollectionControl", "TipsProvider"}:
        with open(f"{objName}.bin", "wb") as f:
            f.write(data.raw_data)
