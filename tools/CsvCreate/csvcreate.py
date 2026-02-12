import os
import csv
import random
from faker import Faker

# --- 配置区 ---
TABLE_COUNT_LIST = [100, 50, 30, 15, 5]        
TABLE_ROWS_LIST = [200, 500, 1000, 5000, 20000]        
MIN_COLS, MAX_COLS = 5, 15               
OUTPUT_ROOT = "./Generated_L10N"

# 目标语言配置
LOCALES = {
    "zh_CN": "zh_CN", "en_US": "en_US", 
    "ja_JP": "ja_JP", "ru_RU": "ru_RU", "ar_SA": "ar_SA"
}

DATA_TYPES = ["int", "float", "string", "int64", "Dict", "Array"]

# 初始化生成器：fk_en 用于专有信息(Dict/Array)，fakers 用于本地化文本(string)
fk_en = Faker("en_US")
fakers = {lang: Faker(locale) for lang, locale in LOCALES.items()}

# 1. 预生成所有表的结构信息
table_configs = []
table_idx = 1
for group_idx, count in enumerate(TABLE_COUNT_LIST):
    for _ in range(count):
        cols_count = random.randint(MIN_COLS, MAX_COLS)
        config = {
            "name": f"TestTable_{table_idx:03d}.csv",
            "rows": TABLE_ROWS_LIST[group_idx],
            "names": ["Id"] + [f"Field_{c}" for c in range(cols_count)],
            "types": ["int"] + [random.choice(DATA_TYPES) for _ in range(cols_count)]
        }
        table_configs.append(config)
        table_idx += 1

def generate_l10n_value(data_type, lang, seed):
    random.seed(seed)
    # 强制让 Dict 和 Array 使用英文生成器，保证全球统一
    if data_type == "Dict":
        items = [f"{fk_en.word()}:{random.randint(1,50)}" for _ in range(random.randint(1, 3))]
        return "|".join(items)
    elif data_type == "Array":
        vals = [fk_en.first_name() for _ in range(random.randint(2, 5))]
        return "|".join(vals)
    
    # 以下为根据语言变化的内容
    fk = fakers[lang]
    if data_type == "int":
        return random.randint(1, 100000)
    elif data_type == "float":
        return round(random.uniform(1.0, 5000.0), 2)
    elif data_type == "int64":
        return random.randint(1000000000, 9000000000)
    elif data_type == "string":
        s = fk.sentence(nb_words=3)
        if random.random() > 0.8: s = f"Text, \"{s}\""
        return s
    return ""

# 2. 按语言生成文件
for lang in LOCALES.keys():
    lang_dir = os.path.join(OUTPUT_ROOT, lang)
    os.makedirs(lang_dir, exist_ok=True)
    print(f"正在生成语言包: [{lang}] ...")

    for config in table_configs:
        file_path = os.path.join(lang_dir, config["name"])
        with open(file_path, 'w', newline='', encoding='utf-8-sig') as f:
            writer = csv.writer(f, quoting=csv.QUOTE_MINIMAL)
            writer.writerow(config["names"])
            writer.writerow(config["types"])
            
            for r in range(config["rows"]):
                row_id = 1000 + r
                row_data = [row_id]
                for c_idx in range(1, len(config["types"])):
                    # 种子依然包含，确保 Dict 内部的随机数值在各语言中也对齐
                    val_seed = f"{config['name']}_{row_id}_{c_idx}"
                    val = generate_l10n_value(config["types"][c_idx], lang, val_seed)
                    row_data.append(val)
                writer.writerow(row_data)

print(f"\n生成完毕！专有信息(Dict/Array)已锁定为英文字符。")