\# 캐릭터 만드는 법



01\. "CharAsset)Rukha93/ModularAnimeCharacter/Prefabs"의 F\_FullBody 또는 M\_FullBody를 복사



02\. 적절히 부품을 켜거나 끄고 material을 설정 후 "03.Characters"에 캐릭터 이름 폴더에 저장, Addressable 이름은 "Character/(캐릭터 이름)/0"

(숫자는 추후 색상을 위한 것이지만 아직 미구현)



03\. "02.Scripts/CharacterComponents"의 CharacterProperty를 부착(필요한 모든 스크립트가 자동으로 부착됨)

-> Character Property의 Character Name 삽입.



04\. (필요 시) Character Property의 Max Hp, Max SA Gauge, Max Drive Gauge 등을 설정



05\. Physics Entity의 기본 Body Box, Hurt Box를 설정 (whiff box는 미구현)



06\. (필요 시) Character Sockets에 잡기 시 부착에 쓰일 소켓 설정



07\. "03.Characters/(캐릭터 이름)/AnimSet"에 AnimSet 생성.



08. "03.Characters/(캐릭터 이름)/Animations"에 필요한 애니메이션 클립 저장. (AnimSet에 있는 것들은 Basic, 아닌 것들은 Additional 폴더에 정리) Addressable 이름은 "(캐릭터 이름)/(클립이름)"



09\. AnimSet에 키와 클립 이름을 연결.

->

(1) Idle, 기본 자세

(2) Crouch, 앉은 자세

(3) WalkF, 앞 걷기

(4) WalkB, 뒤 걷기

5\) JumpUp, 위 점프

(6) JumpF, 앞 점프

(7) JumpB, 뒤 점프

(8) Fall, 추락

(9) Land, 착지

(10) Guard Idle, 서서 가드

(11) Guard Crouch, 앉아 가드

(12) Guard Hit, 가드 중 피격

(13) Guard Hit Crouch, 앉아 가드 중 피격

(14) Hit Ground, 서서 피격

(15) Hit Crouch, 앉아 피격

(16) Hit Air, 공중 피격

(17) Knockdown, 넘어지는 공격을 맞고 날아갈 때의 모션

(18) Knockdown Ground, 넉다운 중 땅에 닿았을 때의 모션

(19) Wake Up, 넉다운 이후 일어나는 모션

(20) Pre Battle, 게임 시작 전 모션

(21) Win, 승리 모션

(22) Lose, 패배 모션

(23) Drive Impact, 드라이브 임팩트

(24) Parry Start, 패리 시작

(25) Parry Loop, 패리 루프

(26) Parry End 패리 끝

(Hard Knockdown은 미구현)

-> 캐릭터에 부착된 Character Animation Config에 AnimSet 삽입



10\. "03.Characters/(캐릭터 이름)/Skills"에 필요한 스킬 생성.

-> 드라이브 임팩트(HP+HK), 드라이브 패리(MP+MK), 각 기본기(LP, MP, HP, LK, MK, HK), 기본 잡기(LP+LK) 필수.

->

\*SkillName : 기술 이름

\*Conditions : 기술 조건 (현재 스킬을 등록 시 연속기 구현, 현재 상태를 등록 시 해당 상태에서 스킬 사용 가능)

\*Command : InputData에서 방향키 조합과 공격 버튼, 뒤 또는 아래 홀드 구현. IsStrict 체크 시 관대한 방향키 인식 해제. Max Frame Gap으로 얼마나 빨리(프레임 단위) 스킬 커맨드를 입력해야하는지 구현.

\*Damage On Hit : 적중 피해.

\*Hitstun Duration : 적중 시 스턴 회복 프레임

\*Blockstun Duration: 가드 시 스턴 회복 프레임

\*Push : 넉백 관련 조정

\*Knockdown : 넉다운 필요 시 Mode를 Trip(바로 넘어짐), Pop Up(날아감)으로 설정. 수치 조절. Techable은 미구현.

\*Drive Gauge Charge Amount, Sa Gauge Charge Amount : 각각 적중 시 게이지 회복량

\*Hit Level : 공격 높이. 공중 공격의 경우 Overhead 선택.

\*Animation Clip Name : 기술 발동 시 애니메이션 클립 이름.

\*Box Lifetimes : 공격 시 박스 생성 및 제거를 처리. 히트박스, 허트박스, 잡기박스 설정. Increment Attack Instance는 연타형 공격 구현. Rehit Cooldown Frame은 다단히트 구현.

\*Throw animation Clip Name : 기술이 잡기일 경우 잡았을 때 모션.

\*Being Thrown Animation Clip Name : 기술이 잡기일 경우 잡힌 적의 모션.

\*Skill Flag : 드라이브 기술일 경우 적절히 지정.

\*Throw Cfg : 잡기 시 피해가 들어가는 타이밍, 잡기 해제 타이밍, 피해량, 히트스턴, 잡기 중 Character Sockets의 소켓에 부착 여부 등을 설정.

\*Spawns Projectiles : 투사체 발사 여부

\*Projectile Spawns : 생성 프레임, 발사될 Character Sockets의 소켓 이름, 투사체 프리팹, 소켓으로부터의 위치, 초기 속도, 중력, 라이프타임, 투사체 스킬(적중 시 스킬에서 피해량 등을 읽어오는 방식이라 투사체 발사 스킬 외에 투사체 자체가 스스로 가지는 스킬이 필요함) 등을 설정.

\*VFX Keys : 시전 시, 적중 시, 가드 시 비주얼 이펙트 이름.

\*Fx Cues : 특정 프레임에 특정 소켓의 비주얼 이펙트 생성 기능

\*SFX : 시전 시, 적중 시, 가드 시 사운드 이펙트 이름

\*Sfx Cues : 특정 프레임에 사운드 이펙트 생성 기능



\*\* 투사체용 스킬의 경우, 적중 시 피해, 히트스턴, 가드스턴, 넉백, 넉다운, 기술 높이, Box Lifetime, vfx, sfx만 설정.

-> 이후 모든 스킬을 우선순위 순으로 Character Property의 All Skills에 삽입.

(기본적으로 기본 잡기 > 드라이브 임팩트 >  드라이브 패리 > 복잡한 기술 > 기본 기술 순)



\*\*\* 투사체는 "03.Characters/(캐릭터 이름)/Prefabs" 폴더에 저장. 투사체는 모델, CharacterProperty, Projectile Controller가 필요.

(현재 투사체가 이상 작동. 해결 방법 고안 중)



11\. "03.Characters/(캐릭터 이름)/Images" 폴더에 캐릭터 선택 창에서 쓰일 일러스트와 헤더 이미지 저장. 둘 다 Texture Type: Sprite, Sprite Mode: Single로 설정. Addressable 이름은 각각 "Illust/(캐릭터 이름)", "Portrait/(캐릭터 이름)"



12\. "03.Characters/Catalog"에 Character Catalog 생성. Entries에 캐릭터 이름, 색상 수(현재 1 고정), AnimSet, Extra Clip Keys 설정. Extra Clip Keys는 AnimSet에 설정되지 않은 애니메이션 이름을 넣음.

-> 타이틀 씬의 Title Preloader에 Character Catalog 삽입.



\# 스테이지 만드는 법

01\. "05.Stages/(스테이지 이름)"에 스테이지 프리팹 생성. Stage Setup 컴포넌트 부착 후 바닥, 벽, 천장 설정. Addressable 이름은 "Stage/(스테이지 이름)"

-> "05.Stages/(스테이지 이름)/Images"에 스테이지 선택창에서 쓰일 일러스트와 헤더 이미지 저장. 둘 다 Texture Type: Sprite, Sprite Mode: Single로 설정. Addressable 이름은 각각 "StageIllust/(스테이지 이름)", "Portrait/(스테이지 이름)"



02\. "05.Stages/Catalog"에 Stage Catalog 생성. 스테이지 이름을 넣음.

-> 타이틀 씬의 Title Preloader에 Stage Catalog 삽입.



\# VFX, SFX

01\. "06.VFXs/Prefabs", "07.Audios/Clips"에 각각 이펙트 저장.

02\. "06.VFXs", "07.Audios"에 각각 Effect Entry\_SO, Sfx Entry\_SO 생성. 키와 프리팹/클립을 넣고 설정.

03\. 각 폴더에 Effect Library\_SO, Audio Library\_SO 생성. 각각에 모든 Entry를 삽입.

-> 타이틀 씬의 FxService, SoundService에 각각 라이브러리 삽입.



