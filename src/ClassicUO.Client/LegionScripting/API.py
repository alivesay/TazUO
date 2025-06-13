JournalEntries = None
Backpack = None
Player = None
Random = None
LastTargetSerial = None
LastTargetPos = None
LastTargetGraphic = None

class ScanType:
    Hostile = 0
    Party = 1
    Followers = 2
    Objects = 3
    Mobiles = 4

class Notoriety:
    Unknown = 1
    Innocent = 1
    Ally = 1
    Gray = 1
    Criminal = 1
    Enemy = 1
    Murderer = 1
    Invulnerable = 1

def ProcessCallbacks():
    pass

def Attack(serial: int):
    pass

def BandageSelf():
    pass

def ClearLeftHand():
    pass

def ClearRightHand():
    pass

def ClickObject(serial: int):
    pass

def UseObject(serial: int, skipQueue: bool = True):
    pass

def Contents(serial: int):
    pass

def ContextMenu(serial: int, entry: int):
    pass

def EquipItem(serial: int):
    pass

def ClearMoveQueue():
    pass

def QueMoveItem(serial: int, destination: int, amt: int = 0, x: int = 0xFFFF, y: int = 0xFFFF):
    pass

def MoveItem(serial: int, destination: int, amt: int = 0, x: int = 0xFFFF, y: int = 0xFFFF):
    pass

def QueMoveItemOffset(serial: int, amt: int = 0, x: int = 0, y: int = 0, z: int = 0):
    pass

def MoveItemOffset(serial: int, amt: int = 0, x: int = 0, y: int = 0, z: int = 0):
    pass

def UseSkill(skillName: str):
    pass

def CastSpell(spellName: str):
    pass

def BuffExists(buffName: str):
    pass

def SysMsg(message: str, hue: int = 946):
    pass

def Msg(message: str):
    pass

def HeadMsg(message: str, serial: int):
    pass

def PartyMsg(message: str):
    pass

def GuildMsg(message: str):
    pass

def AllyMsg(message: str):
    pass

def WhisperMsg(message: str):
    pass

def YellMsg(message: str):
    pass

def EmoteMsg(message: str):
    pass

def FindItem(serial: int):
    pass

def FindType(graphic: int, container: int = 1337, range: int = 1337, hue: int = 1337, minamount: int = 0):
    pass

def FindTypeAll(graphic: int, container: int = 1337, range: int = 1337, hue: int = 1337, minamount: int = 0):
    pass

def FindLayer(layer: str, serial: int = 1337):
    pass

def ItemsInContainer(container: int):
    pass

def UseType(graphic: int, hue: int = 1337, container: int = 1337, skipQueue: bool = True):
    pass

def CreateCooldownBar(seconds: float, text: str, hue: int):
    pass

def IgnoreObject(serial: int):
    pass

def ClearIgnoreList():
    pass

def OnIgnoreList(serial: int):
    pass

def Pathfind(x: int, y: int, z: int = 1337, distance: int = 0):
    pass

def Pathfind(entity: int, distance: int = 0):
    pass

def Pathfinding():
    pass

def CancelPathfinding():
    pass

def AutoFollow(mobile: int):
    pass

def CancelAutoFollow():
    pass

def Run(direction: str):
    pass

def Walk(direction: str):
    pass

def Turn(direction: str):
    pass

def Rename(serial: int, name: str):
    pass

def Dismount():
    pass

def Mount(serial: int):
    pass

def WaitForTarget(targetType: str = "Any", timeout: float = 5):
    pass

def Target(serial: int):
    pass

def Target(x: int, y: int, z: int, graphic: int = 1337):
    pass

def RequestTarget(timeout: float = 5):
    pass

def TargetSelf():
    pass

def TargetLandRel(xOffset: int, yOffset: int):
    pass

def TargetTileRel(xOffset: int, yOffset: int, graphic: int = 1337):
    pass

def CancelTarget():
    pass

def SetSkillLock(skill: str, up_down_locked: str):
    pass

def SetStatLock(stat: str, up_down_locked: str):
    pass

def Logout():
    pass

def ItemNameAndProps(serial: int, wait: bool = False, timeout: int = 10):
    pass

def HasGump(ID: int = 1337):
    pass

def ReplyGump(button: int, gump: int = 1337):
    pass

def CloseGump(ID: int = 1337):
    pass

def GumpContains(text: str, ID: int = 1337):
    pass

def GetGump(ID: int = 1337):
    pass

def ToggleFly():
    pass

def ToggleAbility(ability: str):
    pass

def PrimaryAbilityActive():
    pass

def SecondaryAbilityActive():
    pass

def InJournal(msg: str):
    pass

def InJournalAny(msgs: list[str]):
    pass

def ClearJournal():
    pass

def Pause(seconds: float):
    pass

def Stop():
    pass

def ToggleAutoLoot():
    pass

def Virtue(virtue: str):
    pass

def NearestEntity(scanType: Any, maxDistance: int = 10):
    pass

def NearestMobile(notoriety: list[Any], maxDistance: int = 10):
    pass

def NearestCorpse(distance: int = 3):
    pass

def NearestMobiles(notoriety: list[Any], maxDistance: int = 10):
    pass

def FindMobile(serial: int):
    pass

def GetAllMobiles():
    pass

def GetTile(x: int, y: int):
    pass

def CreateGump(acceptMouseInput: bool = True, canMove: bool = True):
    pass

def AddGump(g: Any):
    pass

def CreateGumpCheckbox(text: str = "", hue: int = 0):
    pass

def CreateGumpLabel(text: str, hue: int = 996):
    pass

def CreateGumpColorBox(opacity: float = 0.7, color: str = "#000000"):
    pass

def CreateGumpItemPic(graphic: int, width: int, height: int):
    pass

def CreateGumpButton(text: str = "", hue: int = 996, normal: int = 0x00EF, pressed: int = 0x00F0, hover: int = 0x00EE):
    pass

def CreateGumpRadioButton(text: str = "", group: int = 0, inactive: int = 0x00D0, active: int = 0x00D1, hue: int = 0xFFFF):
    pass

def CreateGumpTextBox(text: str = "", width: int = 200, height: int = 30, multiline: bool = False):
    pass

def CreateGumpTTFLabel(text: str, size: float, color: str = "#FFFFFF", font: str = TrueTypeLoader.EMBEDDED_FONT, aligned: str = "let", maxWidth: int = 0, applyStroke: bool = False):
    pass

def AddControlOnClick(control: Any, onClick: Any, leftOnly: bool = True):
    pass

def GetSkill(skill: str):
    pass

def DisplayRange(distance: int, hue: int = 22):
    pass

