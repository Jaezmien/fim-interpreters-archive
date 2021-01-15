from enum import Enum
import re
import src.fimGlobal as FGlobal
from typing import Dict

###

class FiMVariableTypes(Enum):
    UNKNOWN = 0,

    NUMBER = 1,
    NUMBER_ARRAY = 2,

    CHAR = 3,
    STRING = 4,
    STRING_ARRAY = 5,

    BOOL = 6,
    BOOL_ARRAY = 7

    @staticmethod
    def IsArray(t, ignore_string = False):
        if ignore_string:
            return t in (FiMVariableTypes.NUMBER_ARRAY, FiMVariableTypes.STRING_ARRAY, FiMVariableTypes.BOOL_ARRAY)
        return t in (FiMVariableTypes.NUMBER_ARRAY, FiMVariableTypes.STRING_ARRAY, FiMVariableTypes.BOOL_ARRAY, FiMVariableTypes.STRING)
    @staticmethod
    def GetArrayType(t):
        if t in (FiMVariableTypes.NUMBER_ARRAY, FiMVariableTypes.STRING_ARRAY, FiMVariableTypes.BOOL_ARRAY, FiMVariableTypes.STRING):
            if t == FiMVariableTypes.NUMBER_ARRAY:
                return FiMVariableTypes.NUMBER
            elif t == FiMVariableTypes.STRING_ARRAY:
                return FiMVariableTypes.STRING
            elif t == FiMVariableTypes.BOOL_ARRAY:
                return FiMVariableTypes.BOOL
            elif t== FiMVariableTypes.STRING:
                return FiMVariableTypes.CHAR
        return t


class FiMVariable:
    def __init__(self, otype: FiMVariableTypes, isConstant = False):
        self.Type = otype
        self.IsConstant = isConstant
    Value = None

    def setv(self,value):
        """ Set value """
        if self.IsConstant and self.Value != None:
            raise Exception("Tried to change a constant variable's value")
        self.Value = value
    def setav(self,index,value):
        """ Set array value at index """
        if self.IsConstant:
            raise Exception("Tried to change a constant variable's value")
        self.Value[index] = value
    def getv(self):
        """ Get raw value. If can convert to int, removes the .0 """
        if self.Type == FiMVariableTypes.NUMBER and self.Value == int(self.Value):
            return int(self.Value)
        return self.Value
    def getsv(self):
        """ Get raw value. If string or char, [1:-1] strips """
        if self.Type == FiMVariableTypes.STRING or self.Type == FiMVariableTypes.CHAR:
            return self.Value[1:-1]
        return self.Value
    def getav(self,index):
        """ Gets array value at index """
        if self.Type == FiMVariableTypes.STRING:
            return f"'{ self.Value[1:-1][index] }'"
        if index in self.Value:
            return self.Value[index]
        return self._defaultArrayValue()
    def _defaultArrayValue(self):
        if self.Type == FiMVariableTypes.STRING_ARRAY:
            return 'nothing'
        elif self.Type == FiMVariableTypes.BOOL_ARRAY:
            return False
        elif self.Type == FiMVariableTypes.NUMBER_ARRAY:
            return 0

    def _isArray(self, ignore_string = False):
        if ignore_string:
            return self.Type in (FiMVariableTypes.STRING_ARRAY, FiMVariableTypes.BOOL_ARRAY, FiMVariableTypes.NUMBER_ARRAY)
        return self.Type in (FiMVariableTypes.STRING_ARRAY, FiMVariableTypes.BOOL_ARRAY, FiMVariableTypes.NUMBER_ARRAY, FiMVariableTypes.STRING)

###

class FiMParagraphParameters:
    def __init__(self, pname: str, ptype: FiMVariableTypes):
        self.Name = pname
        self.Type = ptype
class FiMParagraph:
    Variables = dict()
    
    Parameters: list = None
    Return_Type: FiMVariableTypes = None

    def __init__(self, start_index: int, end_index: int, isMain:bool = False, params:list = None, return_type:FiMVariableTypes = None):
        self.start_index = start_index
        self.end_index = end_index
        self.isMain = isMain
        self.Parameters = params
        self.Return_Type = return_type

###

class FiMMethodReturnTypes(Enum):
    UNKNOWN = 0,
    PRINT = 1,
    VARIABLE_INIT = 2,
    RUN = 3,
    ARRAY_MODIFY = 4,
    VARIABLE_INCREMENT = 5,
    VARIABLE_DECREMENT = 6,
    VARIABLE_REPLACE = 7,
    WHILE = 8,
    RETURN = 9,
    IF_ELSE = 10,
    SWITCH = 11,
    FOR_COUNT = 12,
    FOR_ITER = 13,
    READ = 14,


    D_BREAKPOINT = 69, # ha


class FiMMethodReturn:
    def __init__(self, rtype: FiMMethodReturnTypes, rargs: dict = None):
        self.type = rtype
        self.args = rargs