import src.fimMethods as FMethods
import src.fimGlobal as FGlobal
import src.fimClass as FClass

class FiMArithmeticCheck:
    def __init__(self, is_prefix = False, atype = "Add"):
        self.is_prefix = is_prefix
        self.type = atype

class FiMArithmetic:
    def __init__(self, line, arith:FiMArithmeticCheck):
        if arith.is_prefix:
            prefix_p = FMethods._findFirst(FGlobal.Arithmetic[arith.type]["Prefix"], lambda x: line.startswith(x))
            prefix_i = FMethods._findFirst(FGlobal.Arithmetic[arith.type]["Prefix Infix"], lambda x: f' {x} ' in line)
            line = line[len(prefix_p):].strip()
            self.left_value = line.split(prefix_i)[0].strip()
            self.right_value = line.split(prefix_i)[1].strip()
        else:
            infix_i = FMethods._findFirst(FGlobal.Arithmetic[arith.type]["Infix"], lambda x: f' {x} ' in line)
            self.left_value = line.split(infix_i)[0].strip()
            self.right_value = line.split(infix_i)[1].strip()

        self.left_intr_value = None
        self.rght_intr_value = None
        self.has_calculated_values = False

        cond = {
            "Add": "+",
            "Subtract": "-",
            "Multiply": "*",
            "Divide": "/",
            "Remainder": "%"
        }
        self.sign = cond[ arith.type ]
    def GetValues(self, variables, report):
        self.left_intr_value,l_t = FMethods.BlindConvertStringToProperValue(self.left_value, variables, report)
        self.right_intr_value,r_t = FMethods.BlindConvertStringToProperValue(self.right_value, variables, report)
        if l_t != FClass.FiMVariableTypes.NUMBER or r_t != FClass.FiMVariableTypes.NUMBER:
            raise Exception("Can only use number values in calculation")
        self.has_calculated_values = True

    def Calculate(self) -> bool:
        if not self.has_calculated_values:
            raise Exception("FiMArithmetic variables not calculated")
        return eval( f"{self.left_intr_value} {self.sign} {self.right_intr_value}" )

    @staticmethod
    def IsArithmetic(line: str) -> (bool, FiMArithmeticCheck or None):
        """
            Checks if the line is arithmetic.
            First value is whether it is, and second value if it is prefix.
        """
        for t in ('Add','Subtract','Multiply','Divide','Remainder'):
            # Prefix
            if (FMethods._has(FGlobal.Arithmetic[t]["Prefix"], lambda x: line.startswith(x))
                and FMethods._has(FGlobal.Arithmetic[t]["Prefix Infix"], lambda x: f' {x} ' in line)):
                return True, FiMArithmeticCheck(True, t)
            # Infix
            if "Infix" in FGlobal.Arithmetic[t]:
                if FMethods._has(FGlobal.Arithmetic[t]["Infix"], lambda x: f' {x} ' in line):
                    return True, FiMArithmeticCheck(False, t)
        return False, None

    @staticmethod
    def PrefixCheck(line: str) -> bool:
        """
            Checks if the line contains a prefix arithmetic
        """
        for t in ('Add','Subtract','Multiply','Divide','Remainder'):
            if (FMethods._has(FGlobal.Arithmetic[t]["Prefix"], lambda x: line.startswith(x))
                and FMethods._has(FGlobal.Arithmetic[t]["Prefix Infix"], lambda x: f' {x} ' in line)):
                return True
        return False

class FiMConditional:
    @staticmethod
    def Parse(line:str, variables, report) -> bool:
        and_split = line.split(' and ')
        and_split_result = list(False for _ in range( len(and_split) ))
        and_index = 0
        for and_condition in and_split:
            or_split = and_condition.split(' or ')
            or_split_result = list(False for _ in range( len(or_split) ))
            or_index = 0
            for or_condition in or_split:
                or_split_result[or_index] = FiMConditional.Calculate(or_condition, variables, report)
                or_index += 1
            and_split_result[and_index] = any(or_split_result)
            and_index += 1
        return all( and_split_result )

    @staticmethod
    def GetConditional(line: str) -> (str, str):
        for cond, sign in FGlobal.Conditionals:
            if FMethods._has( FGlobal.Methods[f"Conditional {cond}"], lambda x: f" {x} " in line ):
                return FMethods._findFirst(FGlobal.Methods[f"Conditional {cond}"], lambda x: f" {x} " in line), sign
        raise Exception(f"Invalid conditional {line}")

    @staticmethod
    def Calculate(line, variables, report):
        cond_word,cond_sign = FiMConditional.GetConditional( line )
        c_line = line.split( cond_word )
        left_r_value = c_line[0].strip()
        right_r_value = c_line[1].strip()

        # Remove optional variable type
        left_it,left_il = FMethods.GrabVariableInitializerType(left_r_value)
        if left_it != FClass.FiMVariableTypes.UNKNOWN:
            left_r_value = left_r_value[left_il:].strip()
        right_it,right_il = FMethods.GrabVariableInitializerType(right_r_value)
        if right_it != FClass.FiMVariableTypes.UNKNOWN:
            right_r_value = right_r_value[right_il:].strip()

        left_val,left_type = FMethods.BlindConvertStringToProperValue(left_r_value,variables, report)
        right_val,right_type = FMethods.BlindConvertStringToProperValue(right_r_value, variables, report)

        if FMethods.IsNull( left_val ) and FMethods.IsNull( right_val ):
            return cond_sign == "=="

        if left_type != right_type:
            return cond_sign == "!="

        return eval(f"{left_val} {cond_sign} {right_val}")